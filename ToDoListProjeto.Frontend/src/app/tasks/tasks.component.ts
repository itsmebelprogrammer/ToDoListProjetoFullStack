import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { TaskService } from '../services/task.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NotificationService } from '../services/notification.service';
import { MatDialog } from '@angular/material/dialog';
import { EditTaskDialogComponent } from '../dialogs/edit-task/edit-task.component';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { CreateTaskRequest, TaskItem } from '../models/task-item.model';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatListModule,
    MatIconModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDividerModule,
    MatSelectModule,
    MatChipsModule,
    MatToolbarModule
  ],
  templateUrl: './tasks.component.html',
  styleUrls: ['./tasks.component.css']
})
export class TasksComponent implements OnInit {
  tasks: TaskItem[] = [];
  isLoading = true;
  smartTaskPrompt = '';
  newTask: CreateTaskRequest = {
    title: '',
    description: '',
    priority: 'Média',
    status: 'Pendente'
  };

  private taskService = inject(TaskService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  get pendingTasks(): TaskItem[] {
    return this.tasks.filter(task => task.status !== 'Concluída');
  }

  get completedTasks(): TaskItem[] {
    return this.tasks.filter(task => task.status === 'Concluída');
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.isLoading = true;
    this.taskService.getTasks().subscribe({
      next: (data) => {
        this.tasks = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  openEditDialog(task: TaskItem): void {
    const dialogRef = this.dialog.open(EditTaskDialogComponent, {
      width: '400px',
      data: { task }
    });

    dialogRef.afterClosed().subscribe((result: TaskItem | undefined) => {
      if (result) {
        this.taskService.updateTask(result.id, result).subscribe({
          next: () => {
            const index = this.tasks.findIndex(t => t.id === result.id);
            if (index !== -1) this.tasks[index] = result;
            this.notificationService.showSuccess('Tarefa atualizada com sucesso!');
          },
          error: () => {
            this.notificationService.showError('Falha ao atualizar a tarefa.');
          }
        });
      }
    });
  }

  onAddTask(): void {
    if (!this.newTask.title.trim()) return;

    this.taskService.createTask(this.newTask).subscribe({
      next: (addedTask) => {
        this.tasks.push(addedTask);
        this.newTask = { title: '', description: '', priority: 'Média', status: 'Pendente' };
        this.notificationService.showSuccess('Tarefa adicionada com sucesso!');
      },
      error: () => {}
    });
  }

  onAddSmartTask(): void {
    if (!this.smartTaskPrompt.trim()) return;

    this.taskService.createSmartTask(this.smartTaskPrompt).subscribe({
      next: (addedTask) => {
        this.tasks.push(addedTask);
        this.smartTaskPrompt = '';
        this.notificationService.showSuccess('Tarefa inteligente adicionada!');
      },
      error: () => {
        this.notificationService.showError('Não consegui entender o seu pedido.');
      }
    });
  }

  toggleTaskStatus(task: TaskItem): void {
    const newStatus: 'Pendente' | 'Concluída' = task.status === 'Concluída' ? 'Pendente' : 'Concluída';

    this.taskService.updateTaskStatus(task.id, newStatus).subscribe({
      next: () => {
        task.status = newStatus;
        this.notificationService.showSuccess('Status da tarefa atualizado!');
      },
      error: () => {
        this.notificationService.showError('Falha ao atualizar status.');
      }
    });
  }

  onDeleteTask(taskId: number, index: number): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Exclusão',
        message: 'Tem certeza de que deseja excluir esta tarefa? Esta ação não pode ser desfeita.'
      }
    });

    dialogRef.afterClosed().subscribe((result: boolean) => {
      if (result) {
        this.taskService.deleteTask(taskId).subscribe({
          next: () => {
            this.tasks.splice(index, 1);
            this.notificationService.showSuccess('Tarefa excluída com sucesso.');
          },
          error: () => {
            this.notificationService.showError('Não foi possível excluir a tarefa.');
          }
        });
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
