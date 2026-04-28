import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { errorInterceptor } from './error.interceptor';
import { NotificationService } from '../services/notification.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let notificationSpy: jasmine.SpyObj<NotificationService>;
  let router: Router;

  beforeEach(() => {
    notificationSpy = jasmine.createSpyObj('NotificationService', ['showError']);

    TestBed.configureTestingModule({
      imports: [NoopAnimationsModule],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notificationSpy },
        provideRouter([])
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should redirect to /login and clear token on 401 with active session', () => {
    localStorage.setItem('authToken', 'valid-token');
    const navigateSpy = spyOn(router, 'navigate');

    http.get('/api/tasks').subscribe({ error: () => {} });
    httpMock.expectOne('/api/tasks').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(localStorage.getItem('authToken')).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
    expect(notificationSpy.showError).toHaveBeenCalledWith('Sessão expirada. Faça login novamente.');
  });

  it('should NOT redirect on 401 when there is no token (wrong credentials)', () => {
    const navigateSpy = spyOn(router, 'navigate');

    http.post('/api/auth/login', {}).subscribe({ error: () => {} });
    httpMock.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigateSpy).not.toHaveBeenCalled();
    expect(notificationSpy.showError).not.toHaveBeenCalled();
  });

  it('should show rate limit notification on 429', () => {
    http.get('/api/tasks').subscribe({ error: () => {} });
    httpMock.expectOne('/api/tasks').flush(null, { status: 429, statusText: 'Too Many Requests' });

    expect(notificationSpy.showError).toHaveBeenCalledWith('Muitas tentativas. Aguarde um momento e tente novamente.');
  });

  it('should re-throw the error so components can also handle it', (done) => {
    http.get('/api/tasks').subscribe({
      error: (err) => {
        expect(err.status).toBe(403);
        done();
      }
    });
    httpMock.expectOne('/api/tasks').flush(null, { status: 403, statusText: 'Forbidden' });
  });
});
