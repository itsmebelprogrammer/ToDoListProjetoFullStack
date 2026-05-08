import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notificationService = inject(NotificationService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isRefreshRequest = req.url.includes('/auth/refresh');
      const isLoginRequest = req.url.includes('/auth/login');

      if (error.status === 401 && !isRefreshRequest && !isLoginRequest) {
        const storedRefreshToken = localStorage.getItem('refreshToken');

        if (storedRefreshToken) {
          return authService.refreshToken(storedRefreshToken).pipe(
            switchMap((response) => {
              localStorage.setItem('authToken', response.token);
              localStorage.setItem('refreshToken', response.refreshToken);
              const retryReq = req.clone({
                setHeaders: { Authorization: `Bearer ${response.token}` }
              });
              return next(retryReq);
            }),
            catchError(() => {
              localStorage.removeItem('authToken');
              localStorage.removeItem('refreshToken');
              router.navigate(['/login']);
              notificationService.showError('Sessão expirada. Faça login novamente.');
              return throwError(() => error);
            })
          );
        }

        if (localStorage.getItem('authToken')) {
          localStorage.removeItem('authToken');
          localStorage.removeItem('refreshToken');
          router.navigate(['/login']);
          notificationService.showError('Sessão expirada. Faça login novamente.');
        }
      } else if (error.status === 429) {
        notificationService.showError('Muitas tentativas. Aguarde um momento e tente novamente.');
      }

      return throwError(() => error);
    })
  );
};
