import { TestBed } from '@angular/core/testing';

import { OrderlistService } from './orderlist.service';

describe('OrderlistService', () => {
  let service: OrderlistService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(OrderlistService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
