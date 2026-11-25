import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum LeaveRequestStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveTypeId: number;
  leaveType: string;
  leaveTypeCode?: string;
  startDate: string;
  endDate: string;
  returnDate?: string | null;
  requestedDays: number;
  approvedDays: number;
  reason: string;
  status: LeaveRequestStatus;
  submittedAt: string;
  decisionAt?: string | null;
  decisionBy?: string | null;
  decisionRemarks?: string | null;
}

export interface LeaveRequestListResponse {
  totalCount: number;
  requests: LeaveRequest[];
}

export interface LeaveType {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isPaid: boolean;
  requiresApproval: boolean;
  isActive: boolean;
}

export interface LeaveTypeListResponse {
  totalCount: number;
  leaveTypes: LeaveType[];
}

export interface CreateLeaveRequest {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface CreateLeaveRequestResponse {
  message: string;
  id: number;
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  requestedDays: number;
  status: LeaveRequestStatus;
}

export interface LeaveBalanceLine {
  leaveTypeId: number;
  leaveType: string;
  source: 'CurrentYear' | 'CarryForward';
  payCategory: 'FullPay' | 'HalfPay';
  annualEntitlementDays: number;
  accruedDays: number;
  takenDays: number;
  adjustmentDays: number;
  availableDays: number;
  expiresOn?: string | null;
}

export interface LeaveBalancesResponse {
  leaveYear: number;
  asOf: string;
  balances: LeaveBalanceLine[];
}

export interface LeaveRequestDetails extends LeaveRequest {
  allocations: unknown[];
  approvalHistory: unknown[];
}

@Injectable({
  providedIn: 'root'
})
export class LeaveRequestService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5219/api';

  getMyRequests(): Observable<LeaveRequestListResponse> {
    return this.http.get<LeaveRequestListResponse>(
      `${this.apiUrl}/LeaveRequests`
    );
  }

  getLeaveRequest(id: number): Observable<LeaveRequestDetails> {
    return this.http.get<LeaveRequestDetails>(
      `${this.apiUrl}/LeaveRequests/${id}`
    );
  }

  // HR roles receive every employee's requests from this endpoint.
  getAllRequests(): Observable<LeaveRequestListResponse> {
    return this.http.get<LeaveRequestListResponse>(
      `${this.apiUrl}/LeaveRequests`
    );
  }

  approveLeaveRequest(
    id: number,
    remarks?: string
  ): Observable<{ message: string; approvedDays: number }> {
    return this.http.post<{ message: string; approvedDays: number }>(
      `${this.apiUrl}/LeaveRequests/${id}/approve`,
      { remarks: remarks ?? null }
    );
  }

  rejectLeaveRequest(
    id: number,
    remarks?: string
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/LeaveRequests/${id}/reject`,
      { remarks: remarks ?? null }
    );
  }

  getEmployeeBalances(
    employeeId: number
  ): Observable<LeaveBalancesResponse> {
    return this.http.get<LeaveBalancesResponse>(
      `${this.apiUrl}/LeaveBalances/employee/${employeeId}`
    );
  }

  getMyBalances(): Observable<LeaveBalancesResponse> {
    return this.http.get<LeaveBalancesResponse>(
      `${this.apiUrl}/LeaveBalances/me`
    );
  }

  getActiveLeaveTypes(): Observable<LeaveTypeListResponse> {
    return this.http.get<LeaveTypeListResponse>(
      `${this.apiUrl}/LeaveTypes?isActive=true`
    );
  }

  submitLeaveRequest(
    request: CreateLeaveRequest
  ): Observable<CreateLeaveRequestResponse> {
    return this.http.post<CreateLeaveRequestResponse>(
      `${this.apiUrl}/LeaveRequests`,
      request
    );
  }

  cancelLeaveRequest(
    id: number,
    remarks?: string
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/LeaveRequests/${id}/cancel`,
      { remarks: remarks ?? null }
    );
  }
}
