import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [NoopAnimationsModule],
      providers: [
        NotificationService,
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    });
    service = TestBed.inject(NotificationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('showSuccess()', () => {
    it('should open snackbar with the given message', () => {
      service.showSuccess('Operação realizada!');
      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Operação realizada!',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
    });

    it('should use snackbar-success panel class', () => {
      service.showSuccess('Sucesso!');
      expect(snackBarSpy.open).toHaveBeenCalledWith(
        jasmine.any(String),
        jasmine.any(String),
        jasmine.objectContaining({ panelClass: ['snackbar-success'] })
      );
    });
  });

  describe('showError()', () => {
    it('should open snackbar with the given message', () => {
      service.showError('Ocorreu um erro!');
      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Ocorreu um erro!',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    });

    it('should use snackbar-error panel class', () => {
      service.showError('Erro!');
      expect(snackBarSpy.open).toHaveBeenCalledWith(
        jasmine.any(String),
        jasmine.any(String),
        jasmine.objectContaining({ panelClass: ['snackbar-error'] })
      );
    });

    it('should have longer duration than showSuccess', () => {
      service.showSuccess('ok');
      service.showError('err');
      const successCall = snackBarSpy.open.calls.first().args[2] as any;
      const errorCall = snackBarSpy.open.calls.mostRecent().args[2] as any;
      expect(errorCall.duration).toBeGreaterThan(successCall.duration);
    });
  });
});
