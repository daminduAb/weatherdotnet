import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CityComfortResult {
  cityId: number;
  cityName: string;
  country: string;
  weatherDescription: string;
  weatherIcon: string;
  temperatureCelsius: number;
  humidity: number;
  windSpeed: number;
  cloudiness: number;
  comfortScore: number;
  rank: number;
}

@Injectable({ providedIn: 'root' })
export class WeatherService {
  // The AuthHttpInterceptor (registered in app.config.ts) attaches the
  // Auth0 access token to this request automatically — no manual header needed.
  private readonly baseUrl = '/api/weather';

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<CityComfortResult[]> {
    return this.http.get<CityComfortResult[]>(`${this.baseUrl}/dashboard`);
  }
}
