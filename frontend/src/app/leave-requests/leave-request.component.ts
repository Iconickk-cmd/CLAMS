import { CommonModule } from '@angular/common';
import {
  Component,
  HostListener,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import {
  LeaveBalanceLine,
  LeaveRequest,
  LeaveRequestService,
  LeaveRequestStatus,
  LeaveType
} from '../services/leave-request.service';

interface BalanceGroup {
  leaveTypeId: number;
  leaveType: string;
  available: number;
  totalDays: number;
  takenDays: number;
  percentTaken: number;
  accrued: number;
  entitlement: number;
  hasCarryForward: boolean;
  carryRemaining: number;
  carryOriginal: number;
  halfPayAvailable: number;
}

interface LeaveRequestForm {
  leaveTypeId: number | null;
  startDate: string;
  endDate: string;
  reason: string;
}

@Component({
  selector: 'app-leave-request',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './leave-request.component.html',
  styleUrl: './leave-request.component.scss'
})
export class LeaveRequestComponent implements OnInit {
  private readonly leaveService = inject(LeaveRequestService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly status = LeaveRequestStatus;

  requests: LeaveRequest[] = [];
  balances: LeaveBalanceLine[] = [];
  balanceGroups: BalanceGroup[] = [];
  balancesLoaded = false;
  balancesFailed = false;

  leaveTypes: LeaveType[] = [];

  isLoading = false;
  isLoadingTypes = false;
  isSaving = false;
  hasError = false;
  showForm = false;

  openMenuId: number | null = null;
  detailsRequest: LeaveRequest | null = null;

  message = '';
  errorMessage = '';

  form: LeaveRequestForm = this.emptyForm();

  ngOnInit(): void {
    this.loadLeaveTypes();
    this.loadRequests();
    this.loadBalances();
  }

  @HostListener('document:click')
  closeMenu(): void {
    this.openMenuId = null;
  }

  @HostListener('document:keydown.escape')
  closeOverlays(): void {
    this.openMenuId = null;
    this.detailsRequest = null;
  }

  // ---------------------------------------------------------------- balances

  loadBalances(): void {
    this.leaveService.getMyBalances().subscribe({
      next: response => {
        this.balances = response.balances ?? [];
        this.balanceGroups = this.buildBalanceGroups();
        this.balancesLoaded = true;
        this.balancesFailed = false;
        this.cdr.detectChanges();
      },
      error: error => {
        // Balances are informational; the page still works without them.
        console.error('Leave balances loading error:', error);
        this.balancesLoaded = false;
        this.balancesFailed = true;
        this.cdr.detectChanges();
      }
    });
  }

  /** One card per leave type, combining pay categories and carry-forward. */
  private buildBalanceGroups(): BalanceGroup[] {
    const groups = new Map<number, BalanceGroup>();

    for (const line of this.balances) {
      let group = groups.get(line.leaveTypeId);

      if (!group) {
        group = {
          leaveTypeId: line.leaveTypeId,
          leaveType: line.leaveType,
          available: 0,
          totalDays: 0,
          takenDays: 0,
          percentTaken: 0,
          accrued: 0,
          entitlement: 0,
          hasCarryForward: false,
          carryRemaining: 0,
          carryOriginal: 0,
          halfPayAvailable: 0
        };
        groups.set(line.leaveTypeId, group);
      }

      group.available += line.availableDays;
      group.takenDays += line.takenDays;
      group.totalDays += line.accruedDays + line.adjustmentDays;

      if (line.source === 'CarryForward') {
        group.hasCarryForward = true;
        group.carryRemaining += line.availableDays;
        group.carryOriginal += line.accruedDays;
      } else {
        group.accrued += line.accruedDays;
        group.entitlement += line.annualEntitlementDays;
      }

      if (line.payCategory === 'HalfPay') {
        group.halfPayAvailable += line.availableDays;
      }
    }

    for (const group of groups.values()) {
      group.percentTaken =
        group.totalDays > 0
          ? Math.min(
              100,
              Math.round((group.takenDays / group.totalDays) * 100)
            )
          : 0;
    }

    return Array.from(groups.values());
  }

  /** Total days available for the leave type chosen in the form. */
  get selectedTypeAvailable(): number | null {
    if (!this.form.leaveTypeId || !this.balancesLoaded) {
      return null;
    }

    const lines = this.balances.filter(
      b => b.leaveTypeId === this.form.leaveTypeId
    );

    if (lines.length === 0) {
      return null;
    }

    return lines.reduce((sum, b) => sum + b.availableDays, 0);
  }

  get selectedTypeHasNoBalanceRecord(): boolean {
    return !!this.form.leaveTypeId &&
      this.balancesLoaded &&
      !this.balances.some(b => b.leaveTypeId === this.form.leaveTypeId);
  }

  // ---------------------------------------------------------------- requests

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
        // HR accounts receive everyone's requests from this endpoint;
        // this page is "my leave", so keep only the user's own.
        const me = this.authService.getCurrentUser()?.employeeId;
        const all = response.requests ?? [];
        this.requests = me ? all.filter(r => r.employeeId === me) : all;
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
        this.loadBalances();
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

  canCancel(request: LeaveRequest): boolean {
    return (
      request.status === LeaveRequestStatus.Pending ||
      request.status === LeaveRequestStatus.Approved
    );
  }

  cancelRequest(request: LeaveRequest): void {
    this.openMenuId = null;

    if (!this.canCancel(request)) {
      return;
    }

    if (!confirm(
      `Are you sure you want to cancel this ${request.leaveType} application?`
    )) {
      return;
    }

    this.errorMessage = '';
    this.detailsRequest = null;

    this.leaveService.cancelLeaveRequest(request.id).subscribe({
      next: response => {
        this.message =
          response.message || 'Leave application cancelled successfully.';
        this.loadRequests();
        this.loadBalances();
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

  // ---------------------------------------------------------------- table helpers

  toggleMenu(id: number, event: Event): void {
    event.stopPropagation();
    this.openMenuId = this.openMenuId === id ? null : id;
  }

  viewDetails(request: LeaveRequest): void {
    this.openMenuId = null;
    this.detailsRequest = request;
  }

  closeDetails(): void {
    this.detailsRequest = null;
  }

  /** Display reference such as ANL/0012/2026. */
  leaveNumber(request: LeaveRequest): string {
    const code = (
      request.leaveTypeCode ||
      request.leaveType.replace(/[^A-Za-z]/g, '').substring(0, 3)
    ).toUpperCase();
    const year = new Date(request.submittedAt).getFullYear();

    return `${code}/${String(request.id).padStart(4, '0')}/${year}`;
  }

  /** Next working day after the leave ends (server-calculated). */
  returnDate(request: LeaveRequest): string {
    return request.returnDate
      ? request.returnDate.substring(0, 10)
      : this.nextWeekday(request.endDate);
  }

  // Fallback only: skips weekends but not public holidays.
  private nextWeekday(isoDate: string): string {
    const [y, m, d] = isoDate.substring(0, 10).split('-').map(Number);
    const date = new Date(y, m - 1, d);

    do {
      date.setDate(date.getDate() + 1);
    } while (date.getDay() === 0 || date.getDay() === 6);

    return (
      `${date.getFullYear()}-` +
      `${String(date.getMonth() + 1).padStart(2, '0')}-` +
      `${String(date.getDate()).padStart(2, '0')}`
    );
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
