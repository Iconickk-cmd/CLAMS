import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface LeaveType {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isPaid: boolean;
  requiresApproval: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

interface LeaveTypeResponse {
  totalCount: number;
  leaveTypes: LeaveType[];
}

interface LeaveTypeForm {
  code: string;
  name: string;
  description: string;
  isPaid: boolean;
  requiresApproval: boolean;
}

@Component({
  selector: 'app-leave-type',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-type.component.html',
  styleUrl: './leave-type.component.scss'
})
export class LeaveTypeComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api/leaveTypes';

  leaveTypes: LeaveType[] = [];

  isLoading = false;
  isSaving = false;
  hasError = false;

  searchTerm = '';
  selectedStatus = '';
  selectedPayment = '';

  showForm = false;
  isEditing = false;
  editingId: number | null = null;

  message = '';
  errorMessage = '';

  form: LeaveTypeForm = this.emptyForm();

  ngOnInit(): void {
    this.loadLeaveTypes();
  }

  private emptyForm(): LeaveTypeForm {
    return {
      code: '',
      name: '',
      description: '',
      isPaid: true,
      requiresApproval: true
    };
  }

  loadLeaveTypes(): void {
    this.isLoading = true;
    this.hasError = false;

    this.http.get<LeaveTypeResponse>(
      this.apiUrl
    ).subscribe({
      next: response => {
        this.leaveTypes = response.leaveTypes ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave types loading error:', error);
        this.hasError = true;
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get filteredLeaveTypes(): LeaveType[] {
    let results = [...this.leaveTypes];

    const search = this.searchTerm.trim().toLowerCase();

    if (search) {
      results = results.filter(item =>
        item.name.toLowerCase().includes(search) ||
        item.code.toLowerCase().includes(search) ||
        (item.description ?? '').toLowerCase().includes(search)
      );
    }

    if (this.selectedStatus !== '') {
      const active = this.selectedStatus === 'active';
      results = results.filter(item => item.isActive === active);
    }

    if (this.selectedPayment !== '') {
      const paid = this.selectedPayment === 'paid';
      results = results.filter(item => item.isPaid === paid);
    }

    return results;
  }

  get activeCount(): number {
    return this.leaveTypes.filter(item => item.isActive).length;
  }

  get paidCount(): number {
    return this.leaveTypes.filter(item => item.isPaid).length;
  }

  get unpaidCount(): number {
    return this.leaveTypes.filter(item => !item.isPaid).length;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.selectedPayment = '';
  }

  openAddForm(): void {
    this.form = this.emptyForm();
    this.isEditing = false;
    this.editingId = null;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  openEditForm(item: LeaveType): void {
    this.form = {
      code: item.code,
      name: item.name,
      description: item.description ?? '',
      isPaid: item.isPaid,
      requiresApproval: item.requiresApproval
    };

    this.isEditing = true;
    this.editingId = item.id;
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.errorMessage = '';
  }

  saveLeaveType(): void {
    if (
      !this.form.code.trim() ||
      !this.form.name.trim()
    ) {
      this.errorMessage = 'Leave type code and name are required.';
      return;
    }

    const payload = {
      code: this.form.code.trim().toUpperCase(),
      name: this.form.name.trim(),
      description: this.form.description.trim() || null,
      isPaid: this.form.isPaid,
      requiresApproval: this.form.requiresApproval
    };

    this.isSaving = true;
    this.errorMessage = '';

    const request =
      this.isEditing && this.editingId !== null
        ? this.http.put(
          `${this.apiUrl}/${this.editingId}`,
          payload
        )
        : this.http.post(
          this.apiUrl,
          payload
        );

    request.subscribe({
      next: () => {
        this.isSaving = false;
        this.showForm = false;

        this.message = this.isEditing
          ? 'Leave type updated successfully.'
          : 'Leave type created successfully.';

        this.loadLeaveTypes();
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave type save error:', error);

        this.isSaving = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to save leave type. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  deactivateLeaveType(item: LeaveType): void {
    if (!confirm(
      `Are you sure you want to deactivate ${item.name}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${item.id}/deactivate`,
      {}
    ).subscribe({
      next: () => {
        this.message = `${item.name} has been deactivated.`;
        this.loadLeaveTypes();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ??
          'Unable to deactivate this leave type.';
        this.cdr.detectChanges();
      }
    });
  }

  activateLeaveType(item: LeaveType): void {
    if (!confirm(
      `Are you sure you want to reactivate ${item.name}?`
    )) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${item.id}/activate`,
      {}
    ).subscribe({
      next: () => {
        this.message = `${item.name} has been reactivated.`;
        this.loadLeaveTypes();
      },
      error: error => {
        this.errorMessage =
          error.error?.message ??
          'Unable to reactivate this leave type.';
        this.cdr.detectChanges();
      }
    });
  }

}
