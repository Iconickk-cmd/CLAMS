
import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, inject, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import {
  HttpClient,
  HttpErrorResponse,
  HttpParams
} from '@angular/common/http';

interface Position {
  id: number;
  code: string;
  title: string;
  description?: string | null;
  departmentId: number;
  departmentName: string;
  grade?: string | null;
  approvedHeadcount: number;
  occupiedHeadcount: number;
  vacantHeadcount: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

interface PositionPage {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  positions: Position[];
}

interface Department {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
}

interface DepartmentPage {
  departments: Department[];
}

interface PositionForm {
  code: string;
  title: string;
  description: string;
  departmentId: number | null;
  grade: string;
  approvedHeadcount: number;
}

@Component({
  selector: 'app-position',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './position.component.html',
  styleUrl: './position.component.scss'
})
export class PositionComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api/positions';
  private readonly departmentApiUrl =
    'http://localhost:5219/api/departments';

  positions: Position[] = [];
  departments: Department[] = [];

  loading = false;
  saving = false;

  searchTerm = '';
  statusFilter = 'all';
  departmentFilter = 'all';

  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  showForm = false;
  isEditing = false;
  selectedPositionId: number | null = null;

  form: PositionForm = this.emptyForm();

  notification = '';
  notificationType: 'success' | 'error' = 'success';

  ngOnInit(): void {
    this.loadDepartments();
    this.loadPositions();
  }

  private emptyForm(): PositionForm {
    return {
      code: '',
      title: '',
      description: '',
      departmentId: null,
      grade: '',
      approvedHeadcount: 1
    };
  }

  loadDepartments(): void {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', 100)
      .set('isActive', true);

    this.http.get<DepartmentPage>(
      this.departmentApiUrl,
      { params }
    ).subscribe({
      next: response => {
        this.departments = response.departments ?? [];
        this.cdr.detectChanges();
      },
      error: error => {
        this.showNotification(
          this.getErrorMessage(error, 'Unable to load departments.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  loadPositions(): void {
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

    if (this.departmentFilter !== 'all') {
      params = params.set('departmentId', this.departmentFilter);
    }

    this.http.get<PositionPage>(
      this.apiUrl,
      { params }
    ).subscribe({
      next: response => {
        this.positions = response.positions ?? [];
        this.totalCount = response.totalCount ?? 0;
        this.totalPages = response.totalPages ?? 0;
        this.currentPage = response.page ?? this.currentPage;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        this.loading = false;
        this.showNotification(
          this.getErrorMessage(error, 'Unable to load positions.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  searchPositions(): void {
    this.currentPage = 1;
    this.loadPositions();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadPositions();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = 'all';
    this.departmentFilter = 'all';
    this.currentPage = 1;
    this.loadPositions();
  }

  refresh(): void {
    this.loadDepartments();
    this.loadPositions();
  }

  openAddForm(): void {
    this.isEditing = false;
    this.selectedPositionId = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEditForm(position: Position): void {
    this.isEditing = true;
    this.selectedPositionId = position.id;

    this.form = {
      code: position.code,
      title: position.title,
      description: position.description ?? '',
      departmentId: position.departmentId,
      grade: position.grade ?? '',
      approvedHeadcount: position.approvedHeadcount
    };

    this.showForm = true;
  }

  closeForm(): void {
    if (this.saving) {
      return;
    }

    this.showForm = false;
    this.form = this.emptyForm();
    this.selectedPositionId = null;
  }

  savePosition(formDirective: NgForm): void {
    if (formDirective.invalid || this.saving) {
      formDirective.control.markAllAsTouched();
      return;
    }

    if (this.form.departmentId === null) {
      this.showNotification('Please select a department.', 'error');
      return;
    }

    const payload = {
      code: this.form.code.trim().toUpperCase(),
      title: this.form.title.trim(),
      description: this.form.description.trim() || null,
      departmentId: Number(this.form.departmentId),
      grade: this.form.grade.trim() || null,
      approvedHeadcount: Number(this.form.approvedHeadcount)
    };

    this.saving = true;

    const editing = this.isEditing;

    const request = editing && this.selectedPositionId !== null
      ? this.http.put(
        `${this.apiUrl}/${this.selectedPositionId}`,
        payload
      )
      : this.http.post(this.apiUrl, payload);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.form = this.emptyForm();
        this.selectedPositionId = null;

        this.showNotification(
          editing
            ? 'Position updated successfully.'
            : 'Position registered successfully.',
          'success'
        );

        this.loadPositions();
      },
      error: error => {
        this.saving = false;
        this.showNotification(
          this.getErrorMessage(error, 'Unable to save position.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  activatePosition(position: Position): void {
    if (position.isActive) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${position.id}/activate`,
      {}
    ).subscribe({
      next: () => {
        this.showNotification(
          `${position.title} has been activated.`,
          'success'
        );
        this.loadPositions();
      },
      error: error => {
        this.showNotification(
          this.getErrorMessage(error, 'Unable to activate position.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  deactivatePosition(position: Position): void {
    if (!position.isActive) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to deactivate ${position.title}?`
    );

    if (!confirmed) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${position.id}/deactivate`,
      {}
    ).subscribe({
      next: () => {
        this.showNotification(
          `${position.title} has been deactivated.`,
          'success'
        );
        this.loadPositions();
      },
      error: error => {
        this.showNotification(
          this.getErrorMessage(error, 'Unable to deactivate position.'),
          'error'
        );
        this.cdr.detectChanges();
      }
    });
  }

  changePage(page: number): void {
    if (
      page < 1 ||
      page > this.totalPages ||
      page === this.currentPage
    ) {
      return;
    }

    this.currentPage = page;
    this.loadPositions();
  }

  get activeCount(): number {
    return this.positions.filter(p => p.isActive).length;
  }

  get inactiveCount(): number {
    return this.positions.filter(p => !p.isActive).length;
  }

  get approvedHeadcount(): number {
    return this.positions.reduce(
      (total, position) =>
        total + (position.approvedHeadcount || 0),
      0
    );
  }

  get occupiedHeadcount(): number {
    return this.positions.reduce(
      (total, position) =>
        total + (position.occupiedHeadcount || 0),
      0
    );
  }

  get vacantHeadcount(): number {
    return this.positions.reduce(
      (total, position) =>
        total + (position.vacantHeadcount || 0),
      0
    );
  }

  get firstRecord(): number {
    return this.totalCount === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastRecord(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.totalCount
    );
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
