import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { EditTaskDialogComponent } from './edit-task.component';

describe('EditTaskDialogComponent', () => {
  let component: EditTaskDialogComponent;
  let fixture: ComponentFixture<EditTaskDialogComponent>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<EditTaskDialogComponent>>;

  const mockTask = {
    id: 1,
    title: 'Tarefa de Teste',
    description: 'Descrição da tarefa',
    status: 'Pendente' as const,
    priority: 'Média' as const,
    userId: 'u1',
    createdAt: '2024-01-01'
  };

  beforeEach(async () => {
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [EditTaskDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: { task: mockTask } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EditTaskDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize editData as a copy of the task from dialog data', () => {
    expect(component.editData).toEqual(mockTask);
  });

  it('should not share reference with the original task (deep copy)', () => {
    expect(component.editData).not.toBe(mockTask);
  });

  describe('onCancel()', () => {
    it('should close the dialog without returning data', () => {
      component.onCancel();
      expect(dialogRefSpy.close).toHaveBeenCalled();
      expect(dialogRefSpy.close.calls.mostRecent().args.length).toBe(0);
    });
  });

  describe('onSave()', () => {
    it('should close the dialog returning the edited data', () => {
      component.editData.title = 'Tarefa Modificada';
      component.onSave();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(component.editData);
    });

    it('should return modified status and priority', () => {
      component.editData.status = 'Concluída';
      component.editData.priority = 'Alta';
      component.onSave();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(
        jasmine.objectContaining({ status: 'Concluída', priority: 'Alta' })
      );
    });

    it('should return the task id unchanged', () => {
      component.onSave();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(
        jasmine.objectContaining({ id: 1 })
      );
    });
  });
});
