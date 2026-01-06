export interface Productmodel {
    id: string;
    name: string;
    price: number;
    isActive: boolean;
    unitPrice: number;

}

export interface ProductUpdatemodel extends Partial<Productmodel> {
    productId: string;
    newPrice: number;

}
