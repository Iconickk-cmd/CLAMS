
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import {
  Router,
  RouterOutlet,
  RouterLink,
  NavigationEnd
} from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {

  private readonly router = inject(Router);
  private routerSubscription?: Subscription;

  showBackToDashboard = false;

  ngOnInit(): void {
    this.updateNavigation();

    this.routerSubscription = this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd)
      )
      .subscribe(() => {
        this.updateNavigation();
      });
  }

  private updateNavigation(): void {
    const currentUrl = this.router.url
      .split('?')[0]
      .split('#')[0];

    this.showBackToDashboard =
      currentUrl !== '/' &&
      currentUrl !== '/dashboard' &&
      !currentUrl.startsWith('/login');
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }
}
