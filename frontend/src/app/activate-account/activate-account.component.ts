import {
  Component,
  inject,
  ChangeDetectorRef,
  OnInit
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  AuthService,
  ActivateAccountResponse
} from '../services/auth.service';

@Component({
  selector: 'app-activate-account',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './activate-account.component.html',
  styleUrl: './activate-account.component.scss'
})
export class ActivateAccountComponent implements OnInit {

  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  portalUsername = '';
  activationToken = '';

  temporaryPassword = '';
  newPassword = '';
  confirmPassword = '';

  message = '';
  isLoading = false;
  isActivated = false;

  ngOnInit(): void {
    this.portalUsername =
      this.route.snapshot.queryParamMap.get('username') ?? '';

    this.activationToken =
      this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.portalUsername || !this.activationToken) {
      this.message =
        'The activation link is incomplete. Please contact HR for a new activation link.';
    }
  }

  onActivate(): void {
    this.message = '';

    if (!this.portalUsername || !this.activationToken) {
      this.message =
        'Invalid activation link. Please contact HR for assistance.';
      return;
    }

    if (
      !this.temporaryPassword ||
      !this.newPassword ||
      !this.confirmPassword
    ) {
      this.message = 'Please complete all password fields.';
      return;
    }

    if (this.newPassword.length < 8) {
      this.message =
        'Your new password must contain at least 8 characters.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.message = 'The new passwords do not match.';
      return;
    }

    this.isLoading = true;

    this.authService.activateAccount({
      portalUsername: this.portalUsername,
      activationToken: this.activationToken,
      temporaryPassword: this.temporaryPassword,
      newPassword: this.newPassword,
      confirmPassword: this.confirmPassword
    })
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: ActivateAccountResponse) => {
          if (response.isSuccessful) {
            this.isActivated = true;
            this.temporaryPassword = '';
            this.newPassword = '';
            this.confirmPassword = '';

            this.message =
              response.message ||
              'Your account has been activated successfully. You can now log in.';
          } else {
            this.message =
              response.message || 'Account activation was unsuccessful.';
          }

          this.cdr.detectChanges();
        },

        error: (error: HttpErrorResponse) => {
          const serverMessage =
            error.error &&
            typeof error.error === 'object' &&
            'message' in error.error
              ? String(error.error.message)
              : null;

          this.message =
            serverMessage ??
            'Unable to activate your account. Please try again or contact HR.';

          this.cdr.detectChanges();
        }
      });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
