import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrdercardComponent } from './ordercard.component';

describe('OrdercardComponent', () => {
  let component: OrdercardComponent;
  let fixture: ComponentFixture<OrdercardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrdercardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OrdercardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
