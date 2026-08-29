import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WeatherService, CityComfortResult } from '../../core/services/weather.service';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  cities: CityComfortResult[] = [];
  loading = true;
  error: string | null = null;

  sortKey: keyof CityComfortResult = 'rank';
  filterText = '';

  constructor(private weatherService: WeatherService, public auth: AuthService) {}

  logout(): void {
    this.auth.logout({ logoutParams: { returnTo: window.location.origin } });
  }
  //state of mode
  isDarkMode = false;

toggleDarkMode(): void {
  this.isDarkMode = !this.isDarkMode;
  document.documentElement.classList.toggle('dark', this.isDarkMode);
}

  ngOnInit(): void {
    this.isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
    document.documentElement.classList.toggle('dark', this.isDarkMode);

    this.weatherService.getDashboard().subscribe({
      next: (data) => {
        this.cities = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load weather data. Please try again.';
        this.loading = false;
      }
    });
  }

  get filteredCities(): CityComfortResult[] {
    const filtered = this.filterText
      ? this.cities.filter(c =>
          c.cityName.toLowerCase().includes(this.filterText.toLowerCase()))
      : this.cities;

    return [...filtered].sort((a, b) => {
      const av = a[this.sortKey];
      const bv = b[this.sortKey];
      return typeof av === 'number' && typeof bv === 'number' ? av - bv : 0;
    });
  }

  comfortClass(score: number): string {
    if (score >= 75) return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';
    if (score >= 50) return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200';
    return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
  }
}