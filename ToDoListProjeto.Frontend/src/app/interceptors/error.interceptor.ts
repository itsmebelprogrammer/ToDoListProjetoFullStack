import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !!localStorage.getItem('authToken')) {
        localStorage.removeItem('authToken');
        router.navigate(['/login']);
        notificationService.showError('Sessão expirada. Faça login novamente.');
      } else if (error.status === 429) {
        notificationService.showError('Muitas tentativas. Aguarde um momento e tente novamente.');
      }
      return throwError(() => error);
    })
  );
};
