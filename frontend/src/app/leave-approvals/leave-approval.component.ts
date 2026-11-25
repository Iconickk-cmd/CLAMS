import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import {
  LeaveBalanceLine,
  LeaveRequest,
  LeaveRequestService,
  LeaveRequestStatus
} from '../services/leave-request.service';

type StatusFilter =
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'cancelled'
  | 'all';

@Component({
  selector: 'app-leave-approval',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-approval.component.html',
  styleUrl: './leave-approval.component.scss'
})
export class LeaveApprovalComponent implements OnInit {
  private readonly leaveService = inject(LeaveRequestService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly status = LeaveRequestStatus;

  readonly filters: { key: StatusFilter; label: string }[] = [
    { key: 'pending', label: 'Pending' },
    { key: 'approved', label: 'Approved' },
    { key: 'rejected', label: 'Rejected' },
    { key: 'cancelled', label: 'Cancelled' },
    { key: 'all', label: 'All' }
  ];

  requests: LeaveRequest[] = [];
  activeFilter: StatusFilter = 'pending';
  searchText = '';

  isLoading = false;
  hasError = false;
  message = '';
  errorMessage = '';

  // Review dialog state.
  selected: LeaveRequest | null = null;
  selectedBalances: LeaveBalanceLine[] = [];
  balancesState: 'idle' | 'loading' | 'loaded' | 'failed' = 'idle';
  remarks = '';
  isProcessing = false;
  modalError = '';

  ngOnInit(): void {
    this.loadRequests();
  }

  // HR receives every request from this endpoint. The list is filtered
  // in the browser so the tab counts stay accurate; move filtering to
  // the server if the volume of requests grows large.
  loadRequests(): void {
    this.isLoading = true;
    this.hasError = false;

    this.leaveService.getAllRequests().subscribe({
      next: response => {
        this.requests = response.requests ?? [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave approvals loading error:', error);
        this.hasError = true;
        this.isLoading = false;
        this.errorMessage =
          error.error?.message ??
          'Unable to load leave requests.';
        this.cdr.detectChanges();
      }
    });
  }

  setFilter(filter: StatusFilter): void {
    this.activeFilter = filter;
  }

  private statusFor(filter: StatusFilter): LeaveRequestStatus | null {
    switch (filter) {
      case 'pending':
        return LeaveRequestStatus.Pending;
      case 'approved':
        return LeaveRequestStatus.Approved;
      case 'rejected':
        return LeaveRequestStatus.Rejected;
      case 'cancelled':
        return LeaveRequestStatus.Cancelled;
      default:
        return null;
    }
  }

  countFor(filter: StatusFilter): number {
    const wanted = this.statusFor(filter);
    return wanted === null
      ? this.requests.length
      : this.requests.filter(r => r.status === wanted).length;
  }

  get filteredRequests(): LeaveRequest[] {
    const wanted = this.statusFor(this.activeFilter);
    const term = this.searchText.trim().toLowerCase();

    const list = this.requests.filter(r => {
      const matchesStatus = wanted === null || r.status === wanted;
      const matchesSearch =
        !term ||
        r.employeeName.toLowerCase().includes(term) ||
        r.leaveType.toLowerCase().includes(term);
      return matchesStatus && matchesSearch;
    });

    // Work the pending queue oldest-first.
    if (this.activeFilter === 'pending') {
      return [...list].sort(
        (a, b) =>
          new Date(a.submittedAt).getTime() -
          new Date(b.submittedAt).getTime()
      );
    }

    return list;
  }

  isOwnRequest(request: LeaveRequest | null): boolean {
    if (!request) {
      return false;
    }
    return (
      request.employeeId ===
      this.authService.getCurrentUser()?.employeeId
    );
  }

  openReview(request: LeaveRequest): void {
    this.selected = request;
    this.remarks = '';
    this.modalError = '';
    this.message = '';
    this.selectedBalances = [];
    this.balancesState = 'loading';

    this.leaveService.getEmployeeBalances(request.employeeId).subscribe({
      next: response => {
        // Ignore a late response if the dialog moved on.
        if (this.selected?.id !== request.id) {
          return;
        }
        this.selectedBalances = response.balances ?? [];
        this.balancesState = 'loaded';
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee balances loading error:', error);
        if (this.selected?.id !== request.id) {
          return;
        }
        this.balancesState = 'failed';
        this.cdr.detectChanges();
      }
    });
  }

  closeReview(): void {
    if (this.isProcessing) {
      return;
    }
    this.selected = null;
    this.modalError = '';
  }

  get selectedTypeLines(): LeaveBalanceLine[] {
    const current = this.selected;
    return current
      ? this.selectedBalances.filter(
          b => b.leaveTypeId === current.leaveTypeId
        )
      : [];
  }

  get selectedAvailable(): number {
    return this.selectedTypeLines.reduce(
      (sum, line) => sum + line.availableDays,
      0
    );
  }

  get hasInsufficientBalance(): boolean {
    return (
      !!this.selected &&
      this.balancesState === 'loaded' &&
      this.selectedTypeLines.length > 0 &&
      this.selectedAvailable < this.selected.requestedDays
    );
  }

  getPayCategoryLabel(line: LeaveBalanceLine): string {
    const pay = line.payCategory === 'HalfPay' ? 'Half pay' : 'Full pay';
    return line.source === 'CarryForward'
      ? `${pay} (carried forward)`
      : pay;
  }

  approve(): void {
    const request = this.selected;
    if (!request || this.isProcessing || this.isOwnRequest(request)) {
      return;
    }

    this.isProcessing = true;
    this.modalError = '';

    this.leaveService
      .approveLeaveRequest(request.id, this.remarks.trim() || undefined)
      .subscribe({
        next: response => {
          this.isProcessing = false;
          this.selected = null;
          this.message =
            response.message || 'Leave request approved.';
          this.loadRequests();
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Leave approval error:', error);
          this.isProcessing = false;
          this.modalError =
            error.error?.message ??
            'Unable to approve this leave request.';
          this.cdr.detectChanges();
        }
      });
  }

  reject(): void {
    const request = this.selected;
    if (!request || this.isProcessing || this.isOwnRequest(request)) {
      return;
    }

    const remarks = this.remarks.trim();
    if (remarks.length < 3) {
      this.modalError =
        'Please give a reason for rejecting this request.';
      return;
    }

    this.isProcessing = true;
    this.modalError = '';

    this.leaveService
      .rejectLeaveRequest(request.id, remarks)
      .subscribe({
        next: response => {
          this.isProcessing = false;
          this.selected = null;
          this.message =
            response.message || 'Leave request rejected.';
          this.loadRequests();
          this.cdr.detectChanges();
        },
        error: error => {
          console.error('Leave rejection error:', error);
          this.isProcessing = false;
          this.modalError =
            error.error?.message ??
            'Unable to reject this leave request.';
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
}
