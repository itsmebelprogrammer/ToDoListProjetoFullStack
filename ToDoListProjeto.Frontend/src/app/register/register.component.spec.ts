import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { RegisterComponent } from './register.component';
import { AuthService } from '../auth.service';
import { User } from '../models/user.model';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  const mockUser: User = { id: '1', name: 'Test User', email: 'test@test.com' };

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['register']);

    await TestBed.configureTestingModule({
      imports: [RegisterComponent, NoopAnimationsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty fields', () => {
    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
  });

  describe('register()', () => {
    it('should navigate to /login on successful registration', () => {
      const navigateSpy = spyOn(router, 'navigate');
      authServiceSpy.register.and.returnValue(of(mockUser));
      component.form.setValue({ name: 'Test User', email: 'test@test.com', password: 'Teste@123' });
      component.register();
      expect(navigateSpy).toHaveBeenCalledWith(['/login']);
    });

    it('should show duplicate email error on 400 status', () => {
      authServiceSpy.register.and.returnValue(throwError(() => ({ status: 400 })));
      component.form.setValue({ name: 'Xu', email: 'x@x.com', password: 'Abc@1234' });
      component.register();
      expect(component.errorMessage).toBe('Este e-mail já está cadastrado. Por favor, use outro e-mail.');
    });

    it('should show duplicate email error on 409 status', () => {
      authServiceSpy.register.and.returnValue(throwError(() => ({ status: 409 })));
      component.form.setValue({ name: 'Xu', email: 'x@x.com', password: 'Abc@1234' });
      component.register();
      expect(component.errorMessage).toBe('Este e-mail já está cadastrado. Por favor, use outro e-mail.');
    });

    it('should not set errorMessage on server error (500) — handled by error interceptor', () => {
      authServiceSpy.register.and.returnValue(throwError(() => ({ status: 500 })));
      component.form.setValue({ name: 'Xu', email: 'x@x.com', password: 'Abc@1234' });
      component.register();
      expect(component.errorMessage).toBe('');
    });

    it('should call authService.register with form data', () => {
      authServiceSpy.register.and.returnValue(of(mockUser));
      component.form.setValue({ name: 'Ana', email: 'ana@test.com', password: 'Ana@1234' });
      component.register();
      expect(authServiceSpy.register).toHaveBeenCalledWith({ name: 'Ana', email: 'ana@test.com', password: 'Ana@1234' });
    });
  });
});
