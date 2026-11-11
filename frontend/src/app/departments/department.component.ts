
import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, inject, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';

interface Department {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  employeeCount: number;
  positionCount: number;
  createdAt?: string;
  updatedAt?: string;
}

interface DepartmentPage {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  departments: Department[];
}

interface DepartmentForm {
  code: string;
  name: string;
  description: string;
}

@Component({
  selector: 'app-department',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './department.component.html',
  styleUrl: './department.component.scss'
})
export class DepartmentComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api/departments';

  departments: Department[] = [];

  loading = false;
  saving = false;

  searchTerm = '';
  statusFilter = 'all';

  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  showForm = false;
  isEditing = false;
  selectedDepartmentId: number | null = null;

  form: DepartmentForm = this.emptyForm();

  notification = '';
  notificationType: 'success' | 'error' = 'success';

  ngOnInit(): void {
    this.loadDepartments();
  }

  private emptyForm(): DepartmentForm {
    return {
      code: '',
      name: '',
      description: ''
    };
  }

  loadDepartments(): void {
    this.loading = true;

    let params = new HttpParams()
      .set('page', this.currentPage)
      .set('pageSize', this.pageSize);

    const search = this.searchTerm.trim();

    if (search) {
      params = params.set('search', search);
    }

    if (this.statusFilter === 'active') {
      params = params.set('isActive', true);
    } else if (this.statusFilter === 'inactive') {
      params = params.set('isActive', false);
    }

    this.http.get<DepartmentPage>(this.apiUrl, { params }).subscribe({
      next: response => {
        this.departments = response.departments ?? [];
        this.totalCount = response.totalCount ?? 0;
        this.totalPages = response.totalPages ?? 0;
        this.currentPage = response.page ?? this.currentPage;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.loading = false;
        this.showNotification(
          this.getErrorMessage(error, 'Unable to load departments.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  searchDepartments(): void {
    this.currentPage = 1;
    this.loadDepartments();
  }

  applyStatusFilter(): void {
    this.currentPage = 1;
    this.loadDepartments();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = 'all';
    this.currentPage = 1;
    this.loadDepartments();
  }

  refresh(): void {
    this.loadDepartments();
  }

  openAddForm(): void {
    this.isEditing = false;
    this.selectedDepartmentId = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEditForm(department: Department): void {
    this.isEditing = true;
    this.selectedDepartmentId = department.id;

    this.form = {
      code: department.code,
      name: department.name,
      description: department.description ?? ''
    };

    this.showForm = true;
  }

  closeForm(): void {
    if (this.saving) {
      return;
    }

    this.showForm = false;
    this.form = this.emptyForm();
    this.selectedDepartmentId = null;
  }

  saveDepartment(formDirective: NgForm): void {
    if (formDirective.invalid || this.saving) {
      formDirective.control.markAllAsTouched();
      return;
    }

    const payload = {
      code: this.form.code.trim().toUpperCase(),
      name: this.form.name.trim(),
      description: this.form.description.trim() || null
    };

    this.saving = true;

    const request = this.isEditing && this.selectedDepartmentId !== null
      ? this.http.put(
        `${this.apiUrl}/${this.selectedDepartmentId}`,
        payload
      )
      : this.http.post(this.apiUrl, payload);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.form = this.emptyForm();
        this.selectedDepartmentId = null;

        this.showNotification(
          this.isEditing
            ? 'Department updated successfully.'
            : 'Department registered successfully.',
          'success'
        );

        this.loadDepartments();
      },
      error: error => {
        this.saving = false;
        this.showNotification(
          this.getErrorMessage(error, 'Unable to save department.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  activateDepartment(department: Department): void {
    if (department.isActive) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${department.id}/activate`,
      {}
    ).subscribe({
      next: () => {
        this.showNotification(
          `${department.name} has been activated.`,
          'success'
        );
        this.loadDepartments();
      },
      error: error => {
        this.showNotification(
          this.getErrorMessage(error, 'Unable to activate department.'),
          'error'
        );
      }
    });
  }

  deactivateDepartment(department: Department): void {
    if (!department.isActive) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to deactivate ${department.name}?`
    );

    if (!confirmed) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${department.id}/deactivate`,
      {}
    ).subscribe({
      next: () => {
        this.showNotification(
          `${department.name} has been deactivated.`,
          'success'
        );
        this.loadDepartments();
      },
      error: error => {
        this.showNotification(
          this.getErrorMessage(error, 'Unable to deactivate department.'),
          'error'
        );
      }
    });
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }

    this.currentPage = page;
    this.loadDepartments();
  }

  get activeCount(): number {
    return this.departments.filter(d => d.isActive).length;
  }

  get inactiveCount(): number {
    return this.departments.filter(d => !d.isActive).length;
  }

  get employeeCount(): number {
    return this.departments.reduce(
      (total, department) => total + (department.employeeCount || 0),
      0
    );
  }

  get positionCount(): number {
    return this.departments.reduce(
      (total, department) => total + (department.positionCount || 0),
      0
    );
  }

  get firstRecord(): number {
    return this.totalCount === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastRecord(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(this.totalPages, this.currentPage + 2);

    for (let page = start; page <= end; page++) {
      pages.push(page);
    }

    return pages;
  }

  showNotification(
    message: string,
    type: 'success' | 'error'
  ): void {
    this.notification = message;
    this.notificationType = type;
  }

  dismissNotification(): void {
    this.notification = '';
  }

  private getErrorMessage(
    error: HttpErrorResponse,
    fallback: string
  ): string {
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    return error.error?.message
      ?? error.error?.title
      ?? fallback;
  }
}
