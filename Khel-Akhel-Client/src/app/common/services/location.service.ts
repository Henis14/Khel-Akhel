import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/api-response.model';
import { CountryResponse, StateResponse } from '../models/location.model';

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  constructor(private api: ApiService) {}

  getCountries(): Observable<ApiResponse<CountryResponse[]>> {
    return this.api.get<CountryResponse[]>('location/countries');
  }

  getStates(countryId: number): Observable<ApiResponse<StateResponse[]>> {
    return this.api.get<StateResponse[]>('location/states', { countryId });
  }
}
