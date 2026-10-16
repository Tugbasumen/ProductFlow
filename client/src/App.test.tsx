import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const products = [
  {
    id: 'product-1',
    code: 'PF-001',
    name: 'USB-C Hub',
    price: 100,
    stockQuantity: 10,
    minimumStockLevel: 2,
    categoryId: 'category-1',
    category: { id: 'category-1', name: 'Technology' },
    isActive: true,
  },
  {
    id: 'product-2',
    code: 'PF-002',
    name: 'Desk Lamp',
    price: 50,
    stockQuantity: 8,
    minimumStockLevel: 2,
    categoryId: 'category-1',
    category: { id: 'category-1', name: 'Technology' },
    isActive: true,
  },
]

const customers = [
  { id: 'customer-1', name: 'Elif Demir', email: 'elif@example.test', isActive: true },
]

const orders = [
  {
    id: 'order-1',
    customerId: 'customer-1',
    customerName: 'Elif Demir',
    status: 1,
    totalAmount: 200,
    createdAtUtc: '2026-10-16T10:00:00Z',
    items: [],
  },
  {
    id: 'order-2',
    customerId: 'customer-2',
    customerName: 'Mert Kaya',
    status: 2,
    totalAmount: 50,
    createdAtUtc: '2026-10-15T10:00:00Z',
    items: [],
  },
]

const movements = [
  {
    id: 'movement-1', productId: 'product-1', productCode: 'PF-001',
    productName: 'USB-C Hub', type: 1, quantity: 3,
    description: 'Receiving', createdAtUtc: '2026-10-16T09:00:00Z',
  },
  {
    id: 'movement-2', productId: 'product-2', productCode: 'PF-002',
    productName: 'Desk Lamp', type: 2, quantity: 1,
    description: 'Dispatch', createdAtUtc: '2026-10-16T08:00:00Z',
  },
]

const jsonResponse = (body: unknown) =>
  ({ ok: true, json: async () => body }) as Response

function mockApi() {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'

    if (url === '/api/auth/login' && method === 'POST') {
      return jsonResponse({
        accessToken: 'test-token',
        expiresAtUtc: '2026-10-16T12:00:00Z',
        email: 'admin@example.test',
        roles: ['Admin'],
      })
    }
    if (url === '/api/orders' && method === 'POST') {
      return jsonResponse({ id: 'order-created' })
    }
    if (url.startsWith('/api/products?') && url.includes('pageSize=10')) {
      const query = new URLSearchParams(url.split('?')[1])
      const page = Number(query.get('page') ?? 1)
      return jsonResponse({ items: products, page, totalCount: 25, totalPages: 3 })
    }
    if (url === '/api/products?page=1&pageSize=100') {
      return jsonResponse({ items: products, totalCount: products.length, totalPages: 1 })
    }
    if (url === '/api/products/critical-stock') return jsonResponse([])
    if (url === '/api/categories') {
      return jsonResponse([{ id: 'category-1', name: 'Technology', isActive: true }])
    }
    if (url === '/api/customers') return jsonResponse(customers)
    if (url === '/api/orders') return jsonResponse(orders)
    if (url === '/api/stockmovements') return jsonResponse(movements)

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('ProductFlow dashboard', () => {
  beforeEach(() => {
    sessionStorage.setItem('productflow.accessToken', 'test-token')
    sessionStorage.setItem('productflow.email', 'test@example.test')
    sessionStorage.setItem('productflow.roles', JSON.stringify(['Admin']))
    mockApi()
  })

  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('renders overview metrics and critical stock from API data', async () => {
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Genel Bakış' })).toBeTruthy()
    expect(screen.getByText('Toplam ürün').parentElement?.textContent).toContain('2')
    expect(screen.getByText('Elif Demir')).toBeTruthy()
  })

  it('logs in and persists the access token for this browser session', async () => {
    sessionStorage.clear()
    const user = userEvent.setup()
    render(<App />)

    await user.type(screen.getByLabelText('E-posta'), 'admin@example.test')
    await user.type(screen.getByLabelText('Parola'), 'Valid-Password-123!')
    await user.click(screen.getByRole('button', { name: 'Giriş Yap' }))

    expect(await screen.findByRole('heading', { name: 'Genel Bakış' })).toBeTruthy()
    expect(sessionStorage.getItem('productflow.accessToken')).toBe('test-token')
  })

  it('uses API pagination when navigating product results', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.mocked(fetch)
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Ürünler' }))
    await user.click(screen.getByRole('button', { name: 'Sonraki sayfa' }))

    expect(await screen.findByText('2 / 3 sayfa')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('page=2'),
      expect.objectContaining({ headers: expect.any(Headers) }),
    )
  })

  it('filters orders by status', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Siparişler' }))
    await user.selectOptions(screen.getByLabelText('Durum'), '2')

    const orderList = document.querySelector<HTMLElement>('.order-list')!
    expect(within(orderList).getByText('Mert Kaya')).toBeTruthy()
    expect(within(orderList).queryByText('Elif Demir')).toBeNull()
  })

  it('filters stock movements by type', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Stok Hareketleri' }))
    await user.selectOptions(screen.getByLabelText('Hareket türü'), '1')

    expect(screen.getByText('Receiving')).toBeTruthy()
    expect(screen.queryByText('Dispatch')).toBeNull()
  })

  it('hides product editing actions from Operators', async () => {
    sessionStorage.setItem('productflow.roles', JSON.stringify(['Operator']))
    const user = userEvent.setup()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Ürünler' }))

    expect(screen.queryByRole('button', { name: 'Düzenle' })).toBeNull()
  })

  it('creates an order with multiple product lines', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.mocked(fetch)
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Siparişler' }))
    await user.selectOptions(screen.getByLabelText('Müşteri'), 'customer-1')
    await user.selectOptions(screen.getByLabelText('Ürün'), 'product-1')
    const quantityInput = screen.getByLabelText('Miktar')
    await user.clear(quantityInput)
    await user.type(quantityInput, '2')
    await user.click(screen.getByRole('button', { name: 'Kalemi Ekle' }))
    await user.selectOptions(screen.getByLabelText('Ürün'), 'product-2')
    await user.click(screen.getByRole('button', { name: 'Kalemi Ekle' }))

    expect(screen.getByText('₺250,00')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Siparişi Oluştur' }))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/orders',
        expect.objectContaining({ method: 'POST' }),
      )
    })

    const request = fetchMock.mock.calls.find(
      ([url, init]) => String(url) === '/api/orders' && init?.method === 'POST',
    )
    const body = JSON.parse(String(request?.[1]?.body)) as {
      customerId: string
      items: Array<{ productId: string; quantity: number }>
    }

    expect(body).toEqual({
      customerId: 'customer-1',
      items: [
        { productId: 'product-1', quantity: 2 },
        { productId: 'product-2', quantity: 1 },
      ],
    })
  })
})