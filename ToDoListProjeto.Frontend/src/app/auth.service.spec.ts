import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login()', () => {
    it('should POST to /auth/login with credentials', () => {
      const loginData = { email: 'test@test.com', password: 'Teste@123' };
      const mockResponse = { token: 'mock-jwt-token' };

      service.login(loginData).subscribe(response => {
        expect(response.token).toBe('mock-jwt-token');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginData);
      req.flush(mockResponse);
    });

    it('should return the token from the response', () => {
      const loginData = { email: 'user@test.com', password: 'Pass@123' };

      service.login(loginData).subscribe(response => {
        expect(response).toEqual({ token: 'returned-token' });
      });

      httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush({ token: 'returned-token' });
    });
  });

  describe('register()', () => {
    it('should POST to /auth/register with registration data', () => {
      const registerData = { name: 'Test User', email: 'test@test.com', password: 'Teste@123' };
      const mockResponse = { id: '1', name: 'Test User', email: 'test@test.com' };

      service.register(registerData).subscribe(response => {
        expect(response.id).toBe('1');
        expect(response.email).toBe('test@test.com');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(registerData);
      req.flush(mockResponse);
    });
  });

  describe('logout()', () => {
    it('should remove authToken from localStorage', () => {
      localStorage.setItem('authToken', 'test-token');
      service.logout();
      expect(localStorage.getItem('authToken')).toBeNull();
    });

    it('should not throw when token does not exist', () => {
      expect(() => service.logout()).not.toThrow();
    });
  });
});
