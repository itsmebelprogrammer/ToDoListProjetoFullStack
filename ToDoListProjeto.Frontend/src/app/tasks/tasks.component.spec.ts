import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { TasksComponent } from './tasks.component';
import { TaskService } from '../services/task.service';
import { AuthService } from '../auth.service';
import { NotificationService } from '../services/notification.service';

describe('TasksComponent', () => {
  let component: TasksComponent;
  let fixture: ComponentFixture<TasksComponent>;
  let taskServiceSpy: jasmine.SpyObj<TaskService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;
  let router: Router;

  const mockTasks = [
    { id: 1, title: 'Tarefa Pendente', status: 'Pendente', priority: 'Alta' },
    { id: 2, title: 'Tarefa Concluída', status: 'Concluída', priority: 'Baixa' },
    { id: 3, title: 'Em Andamento', status: 'Em Andamento', priority: 'Média' }
  ];

  beforeEach(async () => {
    taskServiceSpy = jasmine.createSpyObj('TaskService', [
      'getTasks', 'createTask', 'updateTask', 'updateTaskStatus', 'deleteTask', 'createSmartTask'
    ]);
    authServiceSpy = jasmine.createSpyObj('AuthService', ['logout']);
    notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['showSuccess', 'showError']);
    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);

    taskServiceSpy.getTasks.and.returnValue(of([...mockTasks]));

    await TestBed.configureTestingModule({
      imports: [TasksComponent, NoopAnimationsModule],
      providers: [
        { provide: TaskService, useValue: taskServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: MatDialog, useValue: dialogSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TasksComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load tasks on init', () => {
    expect(taskServiceSpy.getTasks).toHaveBeenCalled();
    expect(component.tasks.length).toBe(3);
  });

  describe('pendingTasks getter', () => {
    it('should return tasks with status different from Concluída', () => {
      expect(component.pendingTasks.length).toBe(2);
      expect(component.pendingTasks.every(t => t.status !== 'Concluída')).toBeTrue();
    });
  });

  describe('completedTasks getter', () => {
    it('should return tasks with status equal to Concluída', () => {
      expect(component.completedTasks.length).toBe(1);
      expect(component.completedTasks[0].status).toBe('Concluída');
    });
  });

  describe('onAddTask()', () => {
    it('should not call createTask when title is empty', () => {
      component.newTask.title = '   ';
      component.onAddTask();
      expect(taskServiceSpy.createTask).not.toHaveBeenCalled();
    });

    it('should add task to list on success', () => {
      const created = { id: 4, title: 'Nova Tarefa', status: 'Pendente', priority: 'Média' };
      taskServiceSpy.createTask.and.returnValue(of(created));
      component.newTask.title = 'Nova Tarefa';
      component.onAddTask();
      expect(component.tasks.length).toBe(4);
      expect(component.tasks[3]).toEqual(created);
    });

    it('should reset newTask after successful creation', () => {
      taskServiceSpy.createTask.and.returnValue(of({ id: 4, title: 'X' }));
      component.newTask.title = 'Nova Tarefa';
      component.onAddTask();
      expect(component.newTask.title).toBe('');
      expect(component.newTask.priority).toBe('Média');
      expect(component.newTask.status).toBe('Pendente');
    });

    it('should show success notification after adding', () => {
      taskServiceSpy.createTask.and.returnValue(of({ id: 4, title: 'X' }));
      component.newTask.title = 'Nova Tarefa';
      component.onAddTask();
      expect(notificationServiceSpy.showSuccess).toHaveBeenCalledWith('Tarefa adicionada com sucesso!');
    });
  });

  describe('onAddSmartTask()', () => {
    it('should not call createSmartTask when prompt is empty', () => {
      component.smartTaskPrompt = '';
      component.onAddSmartTask();
      expect(taskServiceSpy.createSmartTask).not.toHaveBeenCalled();
    });

    it('should add task and clear prompt on success', () => {
      const smartTask = { id: 5, title: 'Reunião urgente', status: 'Pendente', priority: 'Alta' };
      taskServiceSpy.createSmartTask.and.returnValue(of(smartTask));
      component.smartTaskPrompt = 'Reunião urgente amanhã';
      component.onAddSmartTask();
      expect(component.tasks.length).toBe(4);
      expect(component.smartTaskPrompt).toBe('');
      expect(notificationServiceSpy.showSuccess).toHaveBeenCalledWith('Tarefa inteligente adicionada!');
    });

    it('should show error notification on failure', () => {
      taskServiceSpy.createSmartTask.and.returnValue(throwError(() => new Error('API error')));
      component.smartTaskPrompt = 'algum texto';
      component.onAddSmartTask();
      expect(notificationServiceSpy.showError).toHaveBeenCalledWith('Não consegui entender o seu pedido.');
    });
  });

  describe('toggleTaskStatus()', () => {
    it('should toggle status from Pendente to Concluída', () => {
      const task = { id: 1, title: 'T', status: 'Pendente' };
      taskServiceSpy.updateTaskStatus.and.returnValue(of(null));
      component.toggleTaskStatus(task);
      expect(taskServiceSpy.updateTaskStatus).toHaveBeenCalledWith(1, 'Concluída');
      expect(task.status).toBe('Concluída');
    });

    it('should toggle status from Concluída to Pendente', () => {
      const task = { id: 2, title: 'T', status: 'Concluída' };
      taskServiceSpy.updateTaskStatus.and.returnValue(of(null));
      component.toggleTaskStatus(task);
      expect(taskServiceSpy.updateTaskStatus).toHaveBeenCalledWith(2, 'Pendente');
      expect(task.status).toBe('Pendente');
    });

    it('should show success notification after toggle', () => {
      const task = { id: 1, title: 'T', status: 'Pendente' };
      taskServiceSpy.updateTaskStatus.and.returnValue(of(null));
      component.toggleTaskStatus(task);
      expect(notificationServiceSpy.showSuccess).toHaveBeenCalledWith('Status da tarefa atualizado!');
    });

    it('should show error notification on failure', () => {
      const task = { id: 1, title: 'T', status: 'Pendente' };
      taskServiceSpy.updateTaskStatus.and.returnValue(throwError(() => new Error('err')));
      component.toggleTaskStatus(task);
      expect(notificationServiceSpy.showError).toHaveBeenCalledWith('Falha ao atualizar status.');
    });
  });

  describe('onDeleteTask()', () => {
    it('should call deleteTask and remove from list when confirmed', () => {
      dialogSpy.open.and.returnValue({ afterClosed: () => of(true) } as any);
      taskServiceSpy.deleteTask.and.returnValue(of(null));
      const initialLength = component.tasks.length;
      component.onDeleteTask(1, 0);
      expect(taskServiceSpy.deleteTask).toHaveBeenCalledWith(1);
      expect(component.tasks.length).toBe(initialLength - 1);
      expect(notificationServiceSpy.showSuccess).toHaveBeenCalledWith('Tarefa excluída com sucesso.');
    });

    it('should NOT call deleteTask when dialog is dismissed', () => {
      dialogSpy.open.and.returnValue({ afterClosed: () => of(false) } as any);
      component.onDeleteTask(1, 0);
      expect(taskServiceSpy.deleteTask).not.toHaveBeenCalled();
    });
  });

  describe('logout()', () => {
    it('should call authService.logout', () => {
      spyOn(router, 'navigate');
      component.logout();
      expect(authServiceSpy.logout).toHaveBeenCalled();
    });

    it('should navigate to /login', () => {
      const navigateSpy = spyOn(router, 'navigate');
      component.logout();
      expect(navigateSpy).toHaveBeenCalledWith(['/login']);
    });
  });
});
