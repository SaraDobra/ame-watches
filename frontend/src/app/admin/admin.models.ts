export interface LoginResponse {
  token: string;
  expiresAt: string;
  fullName: string;
  email: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminImage {
  id: number;
  url: string;
  sortOrder: number;
}

export interface AdminProductListItem {
  id: number;
  name: string;
  slug: string;
  modelCode: string;
  brand: string;
  price: number;
  stock: number;
  isActive: boolean;
  imageUrl: string | null;
}

export interface AdminProductDetail {
  id: number;
  name: string;
  slug: string;
  modelCode: string;
  brandId: number;
  brand: string;
  description: string | null;
  price: number;
  stock: number;
  gender: string;
  type: string;
  caseSizeMm: number | null;
  waterResistanceM: number | null;
  movement: string | null;
  isActive: boolean;
  images: AdminImage[];
}

export interface ProductInput {
  name: string;
  modelCode: string;
  brandId: number;
  description: string | null;
  price: number;
  stock: number;
  gender: string;
  type: string;
  caseSizeMm: number | null;
  waterResistanceM: number | null;
  movement: string | null;
  isActive: boolean;
}

export interface AdminOrderListItem {
  orderNo: string;
  customerName: string;
  phone: string;
  total: number;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  createdAt: string;
  itemCount: number;
}

export interface AdminOrderLine {
  productId: number;
  productName: string;
  modelCode: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface AdminOrderDetail {
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
  warrantyUntil: string | null;
  needsRefund: boolean;
  allowedStatuses: string[];
  items: AdminOrderLine[];
}

export interface DashboardSummary {
  newOrders: number;
  awaitingPayment: number;
  toShip: number;
  ordersToday: number;
  revenueThisMonth: number;
  lowStock: { id: number; name: string; modelCode: string; stock: number }[];
}

export const STATUS_LABELS: Record<string, string> = {
  New: 'E re',
  Confirmed: 'Konfirmuar',
  Shipped: 'Dërguar',
  Delivered: 'Dorëzuar',
  Cancelled: 'Anuluar'
};

export const PAYMENT_LABELS: Record<string, string> = {
  CashOnDelivery: 'Në dorëzim',
  Card: 'Me kartë',
  Pending: 'Në pritje',
  Paid: 'Paguar',
  Failed: 'Dështuar'
};
