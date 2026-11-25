import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LeaveRequest,
  LeaveRequestService,
  LeaveRequestStatus,
  LeaveType
} from '../services/leave-request.service';

interface LeaveRequestForm {
  leaveTypeId: number | null;
  startDate: string;
  endDate: string;
  reason: string;
}

@Component({
  selector: 'app-leave-request',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-request.component.html',
  styleUrl: './leave-request.component.scss'
})
export class LeaveRequestComponent implements OnInit {
  private readonly leaveService = inject(LeaveRequestService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly status = LeaveRequestStatus;

  requests: LeaveRequest[] = [];
  leaveTypes: LeaveType[] = [];

  isLoading = false;
  isLoadingTypes = false;
  isSaving = false;
  hasError = false;
  showForm = false;

  message = '';
  errorMessage = '';

  form: LeaveRequestForm = this.emptyForm();

  ngOnInit(): void {
    this.loadLeaveTypes();
    this.loadRequests();
  }

  private emptyForm(): LeaveRequestForm {
    return {
      leaveTypeId: null,
      startDate: '',
      endDate: '',
      reason: ''
    };
  }

  loadLeaveTypes(): void {
    this.isLoadingTypes = true;

    this.leaveService.getActiveLeaveTypes().subscribe({
      next: response => {
        this.leaveTypes = response.leaveTypes ?? [];
        this.isLoadingTypes = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave types loading error:', error);
        this.isLoadingTypes = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to load active leave types.';
        this.cdr.detectChanges();
      }
    });
  }

  loadRequests(): void {
    this.isLoading = true;
    this.hasError = false;

    this.leaveService.getMyRequests().subscribe({
      next: response => {
        this.requests = response.requests ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave requests loading error:', error);
        this.hasError = true;
        this.isLoading = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to retrieve your leave applications.';
        this.cdr.detectChanges();
      }
    });
  }

  openForm(): void {
    this.form = this.emptyForm();
    this.message = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.errorMessage = '';
  }

  submitRequest(): void {
    if (
      !this.form.leaveTypeId ||
      !this.form.startDate ||
      !this.form.endDate ||
      !this.form.reason.trim()
    ) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }

    if (this.form.endDate < this.form.startDate) {
      this.errorMessage = 'The end date cannot be before the start date.';
      return;
    }

    if (this.form.reason.trim().length < 3) {
      this.errorMessage = 'Please provide a reason of at least 3 characters.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    this.leaveService.submitLeaveRequest({
      leaveTypeId: this.form.leaveTypeId,
      startDate: this.form.startDate,
      endDate: this.form.endDate,
      reason: this.form.reason.trim()
    }).subscribe({
      next: response => {
        this.isSaving = false;
        this.showForm = false;
        this.form = this.emptyForm();
        this.message =
          response.message || 'Leave application submitted successfully.';
        this.loadRequests();
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave request submission error:', error);
        this.isSaving = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to submit your leave application. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  cancelRequest(request: LeaveRequest): void {
    if (
      request.status !== LeaveRequestStatus.Pending &&
      request.status !== LeaveRequestStatus.Approved
    ) {
      return;
    }

    if (!confirm(
      `Are you sure you want to cancel this ${request.leaveType} application?`
    )) {
      return;
    }

    this.errorMessage = '';

    this.leaveService.cancelLeaveRequest(request.id).subscribe({
      next: response => {
        this.message =
          response.message || 'Leave application cancelled successfully.';
        this.loadRequests();
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave request cancellation error:', error);
        this.errorMessage =
          error.error?.message ??
          'Unable to cancel this leave application.';
        this.cdr.detectChanges();
      }
    });
  }

  getStatusLabel(value: LeaveRequestStatus): string {
    switch (value) {
      case LeaveRequestStatus.Pending:
        return 'Pending';
      case LeaveRequestStatus.Approved:
        return 'Approved';
      case LeaveRequestStatus.Rejected:
        return 'Rejected';
      case LeaveRequestStatus.Cancelled:
        return 'Cancelled';
      default:
        return 'Unknown';
    }
  }

  getStatusClass(value: LeaveRequestStatus): string {
    switch (value) {
      case LeaveRequestStatus.Pending:
        return 'pending';
      case LeaveRequestStatus.Approved:
        return 'approved';
      case LeaveRequestStatus.Rejected:
        return 'rejected';
      case LeaveRequestStatus.Cancelled:
        return 'cancelled';
      default:
        return '';
    }
  }

  get pendingCount(): number {
    return this.requests.filter(
      item => item.status === LeaveRequestStatus.Pending
    ).length;
  }

  get approvedCount(): number {
    return this.requests.filter(
      item => item.status === LeaveRequestStatus.Approved
    ).length;
  }

  get rejectedCount(): number {
    return this.requests.filter(
      item => item.status === LeaveRequestStatus.Rejected
    ).length;
  }
}
