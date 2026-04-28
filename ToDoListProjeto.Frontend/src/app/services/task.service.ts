import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateTaskRequest, TaskItem, UpdateTaskRequest } from '../models/task-item.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private apiUrl = `${environment.apiUrl}/Tasks`;

  constructor(private http: HttpClient) {}

  getTasks(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(this.apiUrl);
  }

  createTask(taskData: CreateTaskRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>(this.apiUrl, taskData);
  }

  updateTask(taskId: number, taskData: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${taskId}`, taskData);
  }

  updateTaskStatus(taskId: number, newStatus: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${taskId}`, { status: newStatus });
  }

  deleteTask(taskId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${taskId}`);
  }

  createSmartTask(prompt: string): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.apiUrl}/smart-add`, { prompt });
  }
}
