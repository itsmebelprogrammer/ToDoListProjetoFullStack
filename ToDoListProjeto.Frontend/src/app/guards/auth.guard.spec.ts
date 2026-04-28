import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let router: Router;

  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/tasks' } as RouterStateSnapshot;

  const runGuard = () =>
    TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])]
    });
    router = TestBed.inject(Router);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should allow access when token exists in localStorage', () => {
    localStorage.setItem('authToken', 'valid-token');
    const result = runGuard();
    expect(result).toBe(true);
  });

  it('should deny access when no token exists', () => {
    const result = runGuard();
    expect(result).toBe(false);
  });

  it('should redirect to /login when no token exists', () => {
    const navigateSpy = spyOn(router, 'navigate');
    runGuard();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('should not redirect when token exists', () => {
    localStorage.setItem('authToken', 'valid-token');
    const navigateSpy = spyOn(router, 'navigate');
    runGuard();
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
