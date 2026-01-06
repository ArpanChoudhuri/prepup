import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductaddupdateComponent } from './productaddupdate.component';

describe('ProductaddupdateComponent', () => {
  let component: ProductaddupdateComponent;
  let fixture: ComponentFixture<ProductaddupdateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductaddupdateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductaddupdateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
