import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TaskService } from './task.service';
import { environment } from '../../environments/environment';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/Tasks`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getTasks()', () => {
    it('should send GET request to /Tasks and return task list', () => {
      const mockTasks = [
        { id: 1, title: 'Tarefa 1', status: 'Pendente', priority: 'Média', userId: 'u1', createdAt: '2024-01-01' },
        { id: 2, title: 'Tarefa 2', status: 'Concluída', priority: 'Alta', userId: 'u1', createdAt: '2024-01-01' }
      ];

      service.getTasks().subscribe(tasks => {
        expect(tasks.length).toBe(2);
        expect(tasks).toEqual(mockTasks as any);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockTasks);
    });

    it('should return empty array when no tasks exist', () => {
      service.getTasks().subscribe(tasks => {
        expect(tasks).toEqual([]);
      });

      httpMock.expectOne(apiUrl).flush([]);
    });
  });

  describe('createTask()', () => {
    it('should send POST request to /Tasks with task data', () => {
      const newTask = { title: 'Nova Tarefa', status: 'Pendente', priority: 'Alta' };
      const createdTask = { id: 1, ...newTask, userId: 'u1', createdAt: '2024-01-01' };

      service.createTask(newTask).subscribe(task => {
        expect(task).toEqual(createdTask as any);
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newTask);
      req.flush(createdTask);
    });
  });

  describe('updateTask()', () => {
    it('should send PUT request to /Tasks/{id} with task data', () => {
      const taskId = 1;
      const updateData = { title: 'Tarefa Atualizada', status: 'Em Andamento' };

      service.updateTask(taskId, updateData).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/${taskId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateData);
      req.flush(null, { status: 204, statusText: 'No Content' });
    });
  });

  describe('updateTaskStatus()', () => {
    it('should send PUT request with only status field', () => {
      const taskId = 5;
      const newStatus = 'Concluída';

      service.updateTaskStatus(taskId, newStatus).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/${taskId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ status: newStatus });
      req.flush(null, { status: 204, statusText: 'No Content' });
    });
  });

  describe('deleteTask()', () => {
    it('should send DELETE request to /Tasks/{id}', () => {
      const taskId = 3;

      service.deleteTask(taskId).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/${taskId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content' });
    });
  });

  describe('createSmartTask()', () => {
    it('should send POST to /Tasks/smart-add with prompt payload', () => {
      const prompt = 'Reunião urgente amanhã';
      const mockTask = { id: 10, title: 'Reunião urgente', status: 'Pendente', priority: 'Alta', userId: 'u1', createdAt: '2024-01-01' };

      service.createSmartTask(prompt).subscribe(task => {
        expect(task).toEqual(mockTask as any);
      });

      const req = httpMock.expectOne(`${apiUrl}/smart-add`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ prompt });
      req.flush(mockTask);
    });
  });
});
