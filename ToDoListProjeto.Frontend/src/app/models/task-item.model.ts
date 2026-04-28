export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  status: 'Pendente' | 'Em Andamento' | 'Concluída';
  priority: 'Baixa' | 'Média' | 'Alta';
  userId: string;
  createdAt: string;
  completedAt?: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  status: string;
  priority: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  status?: string;
  priority?: string;
}
