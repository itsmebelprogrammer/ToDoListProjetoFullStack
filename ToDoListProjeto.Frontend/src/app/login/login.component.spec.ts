import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { LoginComponent } from './login.component';
import { AuthService } from '../auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, NoopAnimationsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty fields', () => {
    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('password')?.value).toBe('');
  });

  describe('login()', () => {
    it('should save token to localStorage on success', () => {
      authServiceSpy.login.and.returnValue(of({ token: 'test-token' }));
      component.form.setValue({ email: 'test@test.com', password: 'Teste@123' });
      component.login();
      expect(localStorage.getItem('authToken')).toBe('test-token');
    });

    it('should navigate to /tasks on successful login', () => {
      const navigateSpy = spyOn(router, 'navigate');
      authServiceSpy.login.and.returnValue(of({ token: 'test-token' }));
      component.form.setValue({ email: 'test@test.com', password: 'Teste@123' });
      component.login();
      expect(navigateSpy).toHaveBeenCalledWith(['/tasks']);
    });

    it('should set errorMessage on failed login', () => {
      authServiceSpy.login.and.returnValue(throwError(() => ({ status: 401 })));
      component.form.setValue({ email: 'test@test.com', password: 'wrong' });
      component.login();
      expect(component.errorMessage).toBe('Email ou senha inválidos.');
    });

    it('should call authService.login with form data', () => {
      authServiceSpy.login.and.returnValue(of({ token: 'token' }));
      component.form.setValue({ email: 'user@mail.com', password: 'Pass@123' });
      component.login();
      expect(authServiceSpy.login).toHaveBeenCalledWith({ email: 'user@mail.com', password: 'Pass@123' });
    });
  });
});
