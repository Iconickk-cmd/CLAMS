import { CommonModule, formatDate } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  HostListener,
  OnInit,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { timeout } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ProfileService, MyProfile } from '../services/profile.service';
import {
  LeaveBalanceLine,
  LeaveRequest,
  LeaveRequestService,
  LeaveRequestStatus,
  LeaveType
} from '../services/leave-request.service';
import { EmployeeTabsComponent } from '../shared/employee-tabs.component';

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
  // Display text is prepared here so the template never has to format.
  availableText: string;
  takenText: string;
  totalText: string;
  accruedText: string;
  entitlementText: string;
  carryRemainingText: string;
  carryOriginalText: string;
  halfPayText: string;
}

interface RequestRow {
  id: number;
  leaveNo: string;
  applied: string;
  approved: string;
  isApproved: boolean;
  start: string;
  returnOn: string;
  statusLabel: string;
  statusClass: string;
  canCancel: boolean;
  source: LeaveRequest;
}

interface DetailView {
  id: number;
  leaveNo: string;
  leaveType: string;
  employee: string;
  reason: string;
  start: string;
  returnOn: string;
  applied: string;
  approved: string;
  statusLabel: string;
  statusClass: string;
  comment: string;
  decision: string;
  submitted: string;
  canCancel: boolean;
}

interface LeaveRequestForm {
  leaveTypeId: number | null;
  startDate: string;
  daysApplied: number;
  reason: string;
}

@Component({
  selector: 'app-leave-request',
  standalone: true,
  imports: [CommonModule, FormsModule, EmployeeTabsComponent],
  templateUrl: './leave-request.component.html',
  styleUrl: './leave-request.component.scss'
})
export class LeaveRequestComponent implements OnInit {
  private readonly leaveService = inject(LeaveRequestService);
  private readonly authService = inject(AuthService);
  private readonly profileService = inject(ProfileService);
  private readonly cdr = inject(ChangeDetectorRef);

  requests: LeaveRequest[] = [];
  rows: RequestRow[] = [];
  skippedRows = 0;

  balances: LeaveBalanceLine[] = [];
  balanceGroups: BalanceGroup[] = [];
  balancesLoaded = false;
  balancesFailed = false;
  balanceError = '';

  leaveTypes: LeaveType[] = [];
  myProfile: MyProfile | null = null;
  isLoadingProfile = false;
  profileLoadError = '';

  isLoading = false;
  isLoadingTypes = false;
  isSaving = false;
  hasError = false;
  showForm = false;

  openMenuId: number | null = null;
  details: DetailView | null = null;

  message = '';
  errorMessage = '';

  form: LeaveRequestForm = this.emptyForm();

  ngOnInit(): void {
    this.loadLeaveTypes();
    this.loadRequests();
    this.loadBalances();
    this.loadMyProfile();
  }
  loadMyProfile(): void {
    this.isLoadingProfile = true;
    this.profileLoadError = '';

    this.profileService.getMyProfile().pipe(timeout(20000)).subscribe({
      next: profile => {
        this.myProfile = profile;
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Profile loading error:', error);
        this.profileLoadError = 'Unable to load supervisor details. Please try again.';
        this.isLoadingProfile = false;
        this.cdr.detectChanges();
      }
    });
  }

  @HostListener('document:click')
  closeMenu(): void {
    this.openMenuId = null;
  }

  @HostListener('document:keydown.escape')
  closeOverlays(): void {
    this.openMenuId = null;
    this.details = null;
  }

  // ---------------------------------------------------------------- helpers

  private num(value: unknown): number {
    const n = Number(value);
    return Number.isFinite(n) ? n : 0;
  }

  private fmtNum(value: number): string {
    return String(Math.round(value * 100) / 100);
  }

  private fmtWholeDays(value: number): string {
    return String(Math.floor(value));
  }

  private fmtDate(value?: string | null): string {
    if (!value) {
      return '-';
    }
    try {
      return formatDate(value.substring(0, 10), 'dd MMM yy', 'en-US');
    } catch {
      return '-';
    }
  }

  private fmtDateTime(value?: string | null): string {
    if (!value) {
      return '-';
    }
    try {
      return formatDate(value, 'dd MMM yy, HH:mm', 'en-US');
    } catch {
      return '-';
    }
  }

  private describeError(error: unknown): string {
    const e = error as {
      status?: number;
      name?: string;
      error?: { message?: string };
    };

    if (e?.name === 'TimeoutError') {
      return 'the server did not respond in time';
    }
    if (e?.status === 0) {
      return 'the API could not be reached - is it running?';
    }
    if (e?.status) {
      const detail = e.error?.message ? `: ${e.error.message}` : '';
      return `HTTP ${e.status}${detail}`;
    }
    return 'unexpected error';
  }

  // ---------------------------------------------------------------- balances

  loadBalances(): void {
    this.balancesLoaded = false;
    this.balancesFailed = false;
    this.balanceError = '';

    this.leaveService.getMyBalances().pipe(timeout(20000)).subscribe({
      next: response => {
        try {
          this.balances = (response?.balances ?? []).map(b => ({
            ...b,
            annualEntitlementDays: this.num(b.annualEntitlementDays),
            accruedDays: this.num(b.accruedDays),
            takenDays: this.num(b.takenDays),
            adjustmentDays: this.num(b.adjustmentDays),
            availableDays: this.num(b.availableDays)
          }));
          this.balanceGroups = this.buildBalanceGroups();
          this.balancesLoaded = true;
        } catch (e) {
          console.error('Leave balances display error:', e);
          this.balancesFailed = true;
          this.balanceError = 'the response could not be read';
        }
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave balances loading error:', error);
        this.balancesFailed = true;
        this.balanceError = this.describeError(error);
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
          leaveType: String(line.leaveType ?? ''),
          available: 0,
          totalDays: 0,
          takenDays: 0,
          percentTaken: 0,
          accrued: 0,
          entitlement: 0,
          hasCarryForward: false,
          carryRemaining: 0,
          carryOriginal: 0,
          halfPayAvailable: 0,
          availableText: '0',
          takenText: '0',
          totalText: '0',
          accruedText: '0',
          entitlementText: '0',
          carryRemainingText: '0',
          carryOriginalText: '0',
          halfPayText: '0'
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

      group.availableText = this.fmtWholeDays(group.available);
      group.takenText = this.fmtWholeDays(group.takenDays);
      group.totalText = this.fmtWholeDays(group.totalDays);
      group.accruedText = this.fmtWholeDays(group.accrued);
      group.entitlementText = this.fmtWholeDays(group.entitlement);
      group.carryRemainingText = this.fmtWholeDays(group.carryRemaining);
      group.carryOriginalText = this.fmtWholeDays(group.carryOriginal);
      group.halfPayText = this.fmtWholeDays(group.halfPayAvailable);
    }

    return Array.from(groups.values());
  }

  /** Days available for the leave type chosen in the form, as text. */
  get selectedTypeAvailableText(): string | null {
    if (!this.form.leaveTypeId || !this.balancesLoaded) {
      return null;
    }

    const lines = this.balances.filter(
      b => b.leaveTypeId === this.form.leaveTypeId
    );

    if (lines.length === 0) {
      return null;
    }

    return String(Math.floor(lines.reduce((sum, b) => sum + b.availableDays, 0)));
  }

  get selectedTypeIsEmpty(): boolean {
    const text = this.selectedTypeAvailableText;
    return text !== null && Number(text) <= 0;
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
      daysApplied: 0,
      reason: ''
    };
  }

  loadLeaveTypes(): void {
    this.isLoadingTypes = true;

    this.leaveService.getActiveLeaveTypes().pipe(timeout(20000)).subscribe({
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

    this.leaveService.getMyRequests().pipe(timeout(20000)).subscribe({
      next: response => {
        try {
          // HR accounts receive everyone's requests from this endpoint;
          // this page is "my leave", so keep only the user's own.
          const me = this.authService.getCurrentUser()?.employeeId;
          const all = response?.requests ?? [];
          this.requests = me ? all.filter(r => r.employeeId === me) : all;
          this.buildRows();
          this.isLoading = false;
        } catch (e) {
          console.error('Leave requests display error:', e);
          this.hasError = true;
          this.isLoading = false;
          this.errorMessage =
            'Your leave applications were received but could not be displayed.';
        }
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Leave requests loading error:', error);
        this.hasError = true;
        this.isLoading = false;
        this.errorMessage =
          `Unable to retrieve your leave applications (${this.describeError(error)}).`;
        this.cdr.detectChanges();
      }
    });
  }

  /** Prepare every table row up front so rendering cannot fail on bad data. */
  private buildRows(): void {
    const rows: RequestRow[] = [];
    let skipped = 0;

    for (const request of this.requests) {
      try {
        rows.push(this.toRow(request));
      } catch (e) {
        skipped++;
        console.error('Unable to display leave request:', request, e);
      }
    }

    this.rows = rows;
    this.skippedRows = skipped;
  }

  private toRow(r: LeaveRequest): RequestRow {
    const isApproved = r.status === LeaveRequestStatus.Approved;

    return {
      id: r.id,
      leaveNo: this.leaveNumber(r),
      applied: this.fmtNum(this.num(r.requestedDays)),
      approved: isApproved ? this.fmtNum(this.num(r.approvedDays)) : '-',
      isApproved,
      start: this.fmtDate(r.startDate),
      returnOn: this.fmtDate(this.returnDate(r)),
      statusLabel: this.getStatusLabel(r.status),
      statusClass: this.getStatusClass(r.status),
      canCancel: this.canCancel(r),
      source: r
    };
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
      !Number.isInteger(this.form.daysApplied) ||
      this.form.daysApplied < 1
    ) {
      this.errorMessage = 'Please complete all required fields.';
      return;
    }



    this.isSaving = true;
    this.errorMessage = '';

    this.leaveService.submitLeaveRequest({
      leaveTypeId: this.form.leaveTypeId,
      startDate: this.form.startDate,
      daysApplied: this.form.daysApplied,
      reason: (this.form.reason ?? '').trim()
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

  cancelRow(item: { id: number; leaveNo: string }): void {
    this.openMenuId = null;

    if (!confirm(`Are you sure you want to cancel application ${item.leaveNo}?`)) {
      return;
    }

    this.errorMessage = '';
    this.details = null;

    this.leaveService.cancelLeaveRequest(item.id).subscribe({
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

  // ---------------------------------------------------------------- table + dialog

  toggleMenu(id: number, event: Event): void {
    event.stopPropagation();
    this.openMenuId = this.openMenuId === id ? null : id;
  }

  viewDetails(row: RequestRow): void {
    const r = row.source;

    this.openMenuId = null;
    this.details = {
      id: r.id,
      leaveNo: row.leaveNo,
      leaveType: r.leaveType ?? '',
      employee: r.employeeName ?? '',
      reason: r.reason ? r.reason : '-',
      start: row.start,
      returnOn: row.returnOn,
      applied: row.applied,
      approved: row.approved,
      statusLabel: row.statusLabel,
      statusClass: row.statusClass,
      comment: r.decisionRemarks ? r.decisionRemarks : '-',
      decision: r.decisionBy
        ? `${row.statusLabel} by ${r.decisionBy}` +
          (r.decisionAt ? ` on ${this.fmtDate(r.decisionAt)}` : '')
        : '-',
      submitted: this.fmtDateTime(r.submittedAt),
      canCancel: row.canCancel
    };
  }

  closeDetails(): void {
    this.details = null;
  }

  /** Prints the open application in a clean, brand-styled page. */
  printDetails(): void {
    const d = this.details;
    if (!d) {
      return;
    }

    const rows: [string, string][] = [
      ['Employee', d.employee],
      ['Leave No', d.leaveNo],
      ['Leave Type', d.leaveType],
      ['Reason', d.reason],
      ['Start Date', d.start],
      ['Return Date', d.returnOn],
      ['Days Applied', d.applied],
      ['Days Approved', d.approved],
      ['Status', d.statusLabel],
      ['Decision', d.decision],
      ['Comment', d.comment],
      ['Submitted', d.submitted]
    ];

    const body = rows
      .map(
        ([label, value]) =>
          `<tr><th>${this.escapeHtml(label)}</th><td>${this.escapeHtml(value)}</td></tr>`
      )
      .join('');

    const html =
      '<!doctype html><html><head><meta charset="utf-8">' +
      `<title>${this.escapeHtml(d.leaveNo)}</title>` +
      '<style>' +
      'body{font-family:"Avenir LT Pro",Avenir,"Segoe UI",Arial,sans-serif;color:#17264f;margin:32px;}' +
      'h1{font-size:20px;margin:0 0 4px;}' +
      'p.sub{margin:0 0 20px;color:#687080;font-size:13px;}' +
      'table{width:100%;border-collapse:collapse;}' +
      'th,td{border:1px solid #d9dee8;padding:10px 12px;text-align:left;font-size:14px;vertical-align:top;}' +
      'th{width:30%;background:#f4f6fa;}' +
      'p.foot{margin-top:24px;color:#8a93a4;font-size:12px;}' +
      '</style></head><body>' +
      `<h1>Leave Application ${this.escapeHtml(d.leaveNo)}</h1>` +
      '<p class="sub">CLAMS | Chamira Systems</p>' +
      `<table>${body}</table>` +
      `<p class="foot">Printed ${this.escapeHtml(this.fmtDateTime(new Date().toISOString()))}</p>` +
      '</body></html>';

    const frame = document.createElement('iframe');
    frame.setAttribute('aria-hidden', 'true');
    frame.style.position = 'fixed';
    frame.style.right = '0';
    frame.style.bottom = '0';
    frame.style.width = '0';
    frame.style.height = '0';
    frame.style.border = '0';
    document.body.appendChild(frame);

    const doc = frame.contentWindow?.document;
    if (!doc || !frame.contentWindow) {
      frame.remove();
      return;
    }

    doc.open();
    doc.write(html);
    doc.close();

    setTimeout(() => {
      frame.contentWindow?.focus();
      frame.contentWindow?.print();
      setTimeout(() => frame.remove(), 1500);
    }, 150);
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  /** Display reference such as ANL/0012/2026. */
  leaveNumber(request: LeaveRequest): string {
    const type = String(request.leaveType ?? '');
    const code = String(
      request.leaveTypeCode || type.replace(/[^A-Za-z]/g, '').substring(0, 3)
    ).toUpperCase();
    const submitted = new Date(request.submittedAt);
    const year = isNaN(submitted.getTime())
      ? new Date().getFullYear()
      : submitted.getFullYear();

    return `${code || 'LV'}/${String(request.id).padStart(4, '0')}/${year}`;
  }

  /** Next working day after the leave ends (server-calculated). */
  returnDate(request: LeaveRequest): string {
    if (request.returnDate) {
      return request.returnDate.substring(0, 10);
    }
    return request.endDate ? this.nextWeekday(request.endDate) : '';
  }

  // Fallback only: skips weekends but not public holidays.
  private nextWeekday(isoDate: string): string {
    const [y, m, d] = isoDate.substring(0, 10).split('-').map(Number);
    const date = new Date(y, m - 1, d);

    if (isNaN(date.getTime())) {
      return '';
    }

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
