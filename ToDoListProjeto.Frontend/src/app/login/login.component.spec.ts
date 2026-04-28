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

  it('should initialize loginData with empty fields', () => {
    expect(component.loginData.email).toBe('');
    expect(component.loginData.password).toBe('');
  });

  describe('login()', () => {
    it('should save token to localStorage on success', () => {
      authServiceSpy.login.and.returnValue(of({ token: 'test-token' }));
      component.loginData = { email: 'test@test.com', password: 'Teste@123' };
      component.login();
      expect(localStorage.getItem('authToken')).toBe('test-token');
    });

    it('should navigate to /tasks on successful login', () => {
      const navigateSpy = spyOn(router, 'navigate');
      authServiceSpy.login.and.returnValue(of({ token: 'test-token' }));
      component.loginData = { email: 'test@test.com', password: 'Teste@123' };
      component.login();
      expect(navigateSpy).toHaveBeenCalledWith(['/tasks']);
    });

    it('should set errorMessage on failed login', () => {
      authServiceSpy.login.and.returnValue(throwError(() => ({ status: 401 })));
      component.loginData = { email: 'test@test.com', password: 'wrong' };
      component.login();
      expect(component.errorMessage).toBe('Erro no login');
    });

    it('should call authService.login with form data', () => {
      authServiceSpy.login.and.returnValue(of({ token: 'token' }));
      component.loginData = { email: 'user@mail.com', password: 'Pass@1' };
      component.login();
      expect(authServiceSpy.login).toHaveBeenCalledWith({ email: 'user@mail.com', password: 'Pass@1' });
    });
  });
});
