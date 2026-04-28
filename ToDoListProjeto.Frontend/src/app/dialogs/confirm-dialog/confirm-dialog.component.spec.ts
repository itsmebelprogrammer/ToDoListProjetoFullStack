import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ConfirmDialogComponent } from './confirm-dialog.component';

describe('ConfirmDialogComponent', () => {
  let component: ConfirmDialogComponent;
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<ConfirmDialogComponent>>;

  const mockDialogData = {
    title: 'Confirmar Exclusão',
    message: 'Tem certeza que deseja excluir esta tarefa?'
  };

  beforeEach(async () => {
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: mockDialogData }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should expose title from dialog data', () => {
    expect(component.data.title).toBe('Confirmar Exclusão');
  });

  it('should expose message from dialog data', () => {
    expect(component.data.message).toBe('Tem certeza que deseja excluir esta tarefa?');
  });

  describe('onConfirm()', () => {
    it('should close the dialog with true', () => {
      component.onConfirm();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });
  });

  describe('onDismiss()', () => {
    it('should close the dialog with false', () => {
      component.onDismiss();
      expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });
  });
});
