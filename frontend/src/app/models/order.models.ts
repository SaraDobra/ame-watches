export interface CartItem {
  productId: number;
  slug: string;
  name: string;
  brand: string;
  modelCode: string;
  price: number;
  imageUrl: string | null;
  quantity: number;
  maxQuantity: number;
}

export interface CreateOrderRequest {
  customerName: string;
  phone: string;
  email: string | null;
  address: string;
  city: string;
  paymentMethod: 'CashOnDelivery' | 'Card';
  items: { productId: number; quantity: number }[];
}

export interface OrderCreated {
  orderNo: string;
  accessToken: string;
  total: number;
  paymentUrl: string | null;
}

export interface OrderLine {
  productId: number;
  productName: string;
  modelCode: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  orderNo: string;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  customerName: string;
  phone: string;
  email: string | null;
  address: string;
  city: string;
  subtotal: number;
  shipping: number;
  total: number;
  createdAt: string;
  paidAt: string | null;
  items: OrderLine[];
}

export interface PaymentSessionInfo {
  orderNo: string;
  amount: number;
  currency: string;
  paymentStatus: string;
}
