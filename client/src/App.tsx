import { useEffect, useState } from 'react'
import './App.css'

type Category = {
  id: string
  name: string
  description?: string | null
  isActive?: boolean
}

type Product = {
  id: string
  code: string
  name: string
  description?: string | null
  price: number
  stockQuantity: number
  minimumStockLevel?: number
  categoryId: string
  category?: {
    id: string
    name: string
  }
  isActive?: boolean
}

type Customer = {
  id: string
  name: string
  email: string
  phone?: string | null
  isActive?: boolean
}

type OrderItem = {
  id: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

type Order = {
  id: string
  customerId: string
  customerName: string
  status: number
  totalAmount: number
  createdAtUtc: string
  items: OrderItem[]
}

type StockMovement = {
  id: string
  productId: string
  productCode: string
  productName: string
  type: number
  quantity: number
  description?: string | null
  createdAtUtc: string
}

type ProductFormState = {
  code: string
  name: string
  description: string
  price: string
  stockQuantity: string
  minimumStockLevel: string
  categoryId: string
}

type CustomerFormState = {
  name: string
  email: string
  phone: string
}

type OrderFormState = {
  customerId: string
  productId: string
  quantity: string
}

type StockMovementFormState = {
  productId: string
  type: string
  quantity: string
  description: string
}

type PagedProducts = {
  items: Product[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const emptyProductForm: ProductFormState = {
  code: '',
  name: '',
  description: '',
  price: '0',
  stockQuantity: '0',
  minimumStockLevel: '5',
  categoryId: '',
}

const emptyCustomerForm: CustomerFormState = {
  name: '',
  email: '',
  phone: '',
}

const emptyOrderForm: OrderFormState = {
  customerId: '',
  productId: '',
  quantity: '1',
}

const emptyStockMovementForm: StockMovementFormState = {
  productId: '',
  type: '1',
  quantity: '1',
  description: '',
}

const orderStatusLabels: Record<number, string> = {
  1: 'Bekliyor',
  2: 'Onaylandı',
  3: 'İptal edildi',
}

const formatMoney = (value: number) =>
  new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    maximumFractionDigits: 2,
  }).format(value)

function App() {
  const [products, setProducts] = useState<Product[]>([])
  const [selectableProducts, setSelectableProducts] = useState<Product[]>([])
  const [totalProductCount, setTotalProductCount] = useState(0)
  const [criticalProductCount, setCriticalProductCount] = useState(0)
  const [categories, setCategories] = useState<Category[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [orders, setOrders] = useState<Order[]>([])
  const [stockMovements, setStockMovements] = useState<StockMovement[]>([])
  const [loading, setLoading] = useState(true)
  const [submittingProduct, setSubmittingProduct] = useState(false)
  const [editingProductId, setEditingProductId] = useState<string | null>(null)
  const [submittingCustomer, setSubmittingCustomer] = useState(false)
  const [submittingOrder, setSubmittingOrder] = useState(false)
  const [submittingMovement, setSubmittingMovement] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [productSearch, setProductSearch] = useState('')
  const [productSearchResultCount, setProductSearchResultCount] = useState(0)
  const [productForm, setProductForm] = useState<ProductFormState>(emptyProductForm)
  const [customerForm, setCustomerForm] = useState<CustomerFormState>(emptyCustomerForm)
  const [orderForm, setOrderForm] = useState<OrderFormState>(emptyOrderForm)
  const [movementForm, setMovementForm] =
    useState<StockMovementFormState>(emptyStockMovementForm)

  const fetchDashboard = async (searchTerm = productSearch) => {
    try {
      setLoading(true)
      const [
        productsResponse,
        allProductsResponse,
        criticalProductsResponse,
        categoriesResponse,
        customersResponse,
        ordersResponse,
        movementsResponse,
      ] = await Promise.all([
          fetch(`/api/products?page=1&pageSize=100&search=${encodeURIComponent(searchTerm)}`),
          fetch('/api/products?page=1&pageSize=100'),
          fetch('/api/products/critical-stock'),
          fetch('/api/categories'),
          fetch('/api/customers'),
          fetch('/api/orders'),
          fetch('/api/stockmovements'),
        ])

      if (
        !productsResponse.ok ||
        !allProductsResponse.ok ||
        !criticalProductsResponse.ok ||
        !categoriesResponse.ok ||
        !customersResponse.ok ||
        !ordersResponse.ok ||
        !movementsResponse.ok
      ) {
        throw new Error('Veriler alınamadı.')
      }

      const productsData = (await productsResponse.json()) as PagedProducts
      const allProductsData = (await allProductsResponse.json()) as PagedProducts
      const criticalProductsData = (await criticalProductsResponse.json()) as Product[]
      const categoriesData = (await categoriesResponse.json()) as Category[]
      const customersData = (await customersResponse.json()) as Customer[]
      const ordersData = (await ordersResponse.json()) as Order[]
      const movementsData = (await movementsResponse.json()) as StockMovement[]

      setProducts(productsData.items ?? [])
      setProductSearchResultCount(productsData.totalCount ?? 0)
      setSelectableProducts(allProductsData.items ?? [])
      setTotalProductCount(allProductsData.totalCount ?? 0)
      setCriticalProductCount(criticalProductsData.length)
      setCategories(categoriesData ?? [])
      setCustomers(customersData ?? [])
      setOrders(ordersData ?? [])
      setStockMovements(movementsData ?? [])

      if (categoriesData.length > 0 && !productForm.categoryId) {
        setProductForm((current) => ({
          ...current,
          categoryId: categoriesData[0].id,
        }))
      }

      if (customersData.length > 0 && !orderForm.customerId) {
        setOrderForm((current) => ({
          ...current,
          customerId: customersData[0].id,
        }))
      }

      if (allProductsData.items.length > 0 && !movementForm.productId) {
        setMovementForm((current) => ({
          ...current,
          productId: allProductsData.items[0].id,
        }))
      }
    } catch (err) {
      setMessage(
        err instanceof Error
          ? err.message
          : 'Beklenmeyen bir hata oluştu.',
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void fetchDashboard()
  }, [])

  const handleProductInputChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = event.target
    setProductForm((current) => ({ ...current, [name]: value }))
  }

  const handleCustomerInputChange = (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const { name, value } = event.target
    setCustomerForm((current) => ({ ...current, [name]: value }))
  }

  const handleOrderInputChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = event.target
    setOrderForm((current) => ({ ...current, [name]: value }))
  }

  const handleMovementInputChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = event.target
    setMovementForm((current) => ({ ...current, [name]: value }))
  }

  const handleProductSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!editingProductId && !productForm.categoryId) {
      setMessage('Önce bir kategori seçmelisiniz.')
      return
    }

    try {
      setSubmittingProduct(true)
      setMessage(null)

      const response = await fetch(
        editingProductId ? `/api/products/${editingProductId}` : '/api/products',
        {
        method: editingProductId ? 'PUT' : 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(
          editingProductId
            ? {
                name: productForm.name,
                description: productForm.description,
                price: Number(productForm.price),
                minimumStockLevel: Number(productForm.minimumStockLevel),
              }
            : {
                code: productForm.code,
                name: productForm.name,
                description: productForm.description,
                price: Number(productForm.price),
                stockQuantity: Number(productForm.stockQuantity),
                minimumStockLevel: Number(productForm.minimumStockLevel),
                categoryId: productForm.categoryId,
              },
        ),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(
          errorPayload?.message ?? 'Ürün kaydedilemedi.',
        )
      }

      setProductForm({ ...emptyProductForm, categoryId: categories[0]?.id ?? '' })
      setEditingProductId(null)
      setMessage(editingProductId ? 'Ürün güncellendi.' : 'Ürün başarıyla eklendi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(
        err instanceof Error ? err.message : 'Ürün eklenirken hata oluştu.',
      )
    } finally {
      setSubmittingProduct(false)
    }
  }

  const beginProductEdit = (product: Product) => {
    setEditingProductId(product.id)
    setProductForm({
      code: product.code,
      name: product.name,
      description: product.description ?? '',
      price: String(product.price),
      stockQuantity: String(product.stockQuantity),
      minimumStockLevel: String(product.minimumStockLevel ?? 5),
      categoryId: product.categoryId,
    })
    setMessage(null)
  }

  const cancelProductEdit = () => {
    setEditingProductId(null)
    setProductForm({ ...emptyProductForm, categoryId: categories[0]?.id ?? '' })
  }

  const handleProductSearch = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void fetchDashboard(productSearch.trim())
  }

  const handleCustomerSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    try {
      setSubmittingCustomer(true)
      setMessage(null)

      const response = await fetch('/api/customers', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: customerForm.name,
          email: customerForm.email,
          phone: customerForm.phone,
        }),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(
          errorPayload?.message ?? 'Müşteri kaydı oluşturulamadı.',
        )
      }

      setCustomerForm(emptyCustomerForm)
      setMessage('Müşteri başarıyla eklendi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(
        err instanceof Error ? err.message : 'Müşteri eklenirken hata oluştu.',
      )
    } finally {
      setSubmittingCustomer(false)
    }
  }

  const handleOrderSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!orderForm.customerId || !orderForm.productId) {
      setMessage('Müşteri ve ürün seçimi zorunludur.')
      return
    }

    const quantity = Number(orderForm.quantity)
    if (!Number.isFinite(quantity) || quantity <= 0) {
      setMessage('Sipariş miktarı 1 ve üzeri olmalıdır.')
      return
    }

    try {
      setSubmittingOrder(true)
      setMessage(null)

      const response = await fetch('/api/orders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerId: orderForm.customerId,
          items: [{ productId: orderForm.productId, quantity }],
        }),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Sipariş oluşturulamadı.')
      }

      setOrderForm(emptyOrderForm)
      setMessage('Sipariş başarıyla oluşturuldu.')
      await fetchDashboard()
    } catch (err) {
      setMessage(
        err instanceof Error ? err.message : 'Sipariş oluşturulurken hata oluştu.',
      )
    } finally {
      setSubmittingOrder(false)
    }
  }

  const updateOrderStatus = async (orderId: string, action: 'confirm' | 'cancel') => {
    try {
      setMessage(null)
      const response = await fetch(`/api/orders/${orderId}/${action}`, {
        method: 'PATCH',
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Sipariş durumu güncellenemedi.')
      }

      setMessage(action === 'confirm' ? 'Sipariş onaylandı.' : 'Sipariş iptal edildi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Sipariş güncellenemedi.')
    }
  }

  const handleMovementSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!movementForm.productId) {
      setMessage('Stok hareketi için ürün seçmelisiniz.')
      return
    }

    try {
      setSubmittingMovement(true)
      setMessage(null)
      const response = await fetch('/api/stockmovements', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          productId: movementForm.productId,
          type: Number(movementForm.type),
          quantity: Number(movementForm.quantity),
          description: movementForm.description,
        }),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Stok hareketi kaydedilemedi.')
      }

      setMovementForm((current) => ({ ...emptyStockMovementForm, productId: current.productId }))
      setMessage('Stok hareketi kaydedildi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Stok hareketi kaydedilemedi.')
    } finally {
      setSubmittingMovement(false)
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">ProductFlow</p>
          <h1>Stok ve Sipariş Takip Paneli</h1>
        </div>
        <button
          type="button"
          className="primary-button"
          onClick={() => void fetchDashboard()}
        >
          Yenile
        </button>
      </header>

      <section className="stats-grid">
        <article className="stat-card">
          <span>Toplam ürün</span>
          <strong>{totalProductCount}</strong>
        </article>
        <article className="stat-card accent">
          <span>Kritik stok</span>
          <strong>{criticalProductCount}</strong>
        </article>
        <article className="stat-card">
          <span>Müşteri sayısı</span>
          <strong>{customers.length}</strong>
        </article>
        <article className="stat-card">
          <span>Sipariş sayısı</span>
          <strong>{orders.length}</strong>
        </article>
      </section>

      {message && <div className="alert">{message}</div>}

      <section className="content-grid">
        <div className="panel">
          <div className="panel-header">
            <h2>Ürünler</h2>
            <span>
              {productSearch
                ? `${productSearchResultCount} eşleşme`
                : `${totalProductCount} kayıt`}
            </span>
          </div>

          <form className="search-form" onSubmit={handleProductSearch}>
            <input
              type="search"
              value={productSearch}
              onChange={(event) => setProductSearch(event.target.value)}
              placeholder="Ürün adı veya kodu ara"
              aria-label="Ürün adı veya kodu ara"
            />
            <button type="submit" className="primary-button">Ara</button>
          </form>

          {loading ? (
            <p className="empty-state">Ürünler yükleniyor...</p>
          ) : products.length === 0 ? (
            <p className="empty-state">Gösterilecek ürün bulunamadı.</p>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Kod</th>
                    <th>Ürün</th>
                    <th>Kategori</th>
                    <th>Fiyat</th>
                    <th>Stok</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {products.map((product) => (
                    <tr key={product.id}>
                      <td>{product.code}</td>
                      <td>
                        <div className="product-name-group">
                          <strong>{product.name}</strong>
                          {product.description && (
                            <small>{product.description}</small>
                          )}
                        </div>
                      </td>
                      <td>{product.category?.name ?? '—'}</td>
                      <td>{formatMoney(product.price)}</td>
                      <td>
                        <span
                          className={
                            product.stockQuantity <= (product.minimumStockLevel ?? 0)
                              ? 'stock-badge danger'
                              : 'stock-badge'
                          }
                        >
                          {product.stockQuantity}
                        </span>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="small-button"
                          onClick={() => beginProductEdit(product)}
                        >
                          Düzenle
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <aside className="panel side-panel">
          <div className="panel-header">
            <h2>{editingProductId ? 'Ürünü Düzenle' : 'Yeni Ürün'}</h2>
          </div>

          <form className="product-form" onSubmit={handleProductSubmit}>
            {!editingProductId && (
              <label>
                Ürün Kodu
                <input
                  name="code"
                  value={productForm.code}
                  onChange={handleProductInputChange}
                  placeholder="PRD-100"
                  required
                />
              </label>
            )}

            <label>
              Ürün Adı
              <input
                name="name"
                value={productForm.name}
                onChange={handleProductInputChange}
                placeholder="Ürün adı"
                required
              />
            </label>

            {!editingProductId && (
              <label>
                Kategori
                <select
                  name="categoryId"
                  value={productForm.categoryId}
                  onChange={handleProductInputChange}
                  required
                >
                  <option value="">Kategori seçin</option>
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </label>
            )}

            <label>
              Açıklama
              <textarea
                name="description"
                value={productForm.description}
                onChange={handleProductInputChange}
                rows={3}
                placeholder="İsteğe bağlı açıklama"
              />
            </label>

            <div className="two-column">
              <label>
                Fiyat
                <input
                  name="price"
                  type="number"
                  min="0"
                  step="0.01"
                  value={productForm.price}
                  onChange={handleProductInputChange}
                  required
                />
              </label>

              {editingProductId ? (
                <label>
                  Kritik stok seviyesi
                  <input
                    name="minimumStockLevel"
                    type="number"
                    min="0"
                    step="1"
                    value={productForm.minimumStockLevel}
                    onChange={handleProductInputChange}
                    required
                  />
                </label>
              ) : (
                <label>
                  Stok
                  <input
                    name="stockQuantity"
                    type="number"
                    min="0"
                    step="1"
                    value={productForm.stockQuantity}
                    onChange={handleProductInputChange}
                    required
                  />
                </label>
              )}
            </div>

            <button type="submit" className="primary-button" disabled={submittingProduct}>
              {submittingProduct
                ? 'Kaydediliyor...'
                : editingProductId
                  ? 'Değişiklikleri Kaydet'
                  : 'Ürünü Kaydet'}
            </button>
            {editingProductId && (
              <button type="button" className="small-button secondary" onClick={cancelProductEdit}>
                Düzenlemeyi İptal Et
              </button>
            )}
          </form>
        </aside>
      </section>

      <section className="bottom-grid">
        <div className="panel">
          <div className="panel-header">
            <h2>Müşteriler</h2>
          </div>

          <form className="product-form" onSubmit={handleCustomerSubmit}>
            <label>
              Müşteri Adı
              <input
                name="name"
                value={customerForm.name}
                onChange={handleCustomerInputChange}
                placeholder="Müşteri adı"
                required
              />
            </label>

            <label>
              E-posta
              <input
                name="email"
                type="email"
                value={customerForm.email}
                onChange={handleCustomerInputChange}
                placeholder="ornek@mail.com"
                required
              />
            </label>

            <label>
              Telefon
              <input
                name="phone"
                value={customerForm.phone}
                onChange={handleCustomerInputChange}
                placeholder="(555) 123 45 67"
              />
            </label>

            <button type="submit" className="primary-button" disabled={submittingCustomer}>
              {submittingCustomer ? 'Ekleniyor...' : 'Müşteri Ekle'}
            </button>
          </form>

          <ul className="info-list">
            {customers.map((customer) => (
              <li key={customer.id}>
                <strong>{customer.name}</strong>
                <span>{customer.email}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="panel">
          <div className="panel-header">
            <h2>Siparişler</h2>
          </div>

          <form className="product-form" onSubmit={handleOrderSubmit}>
            <label>
              Müşteri
              <select
                name="customerId"
                value={orderForm.customerId}
                onChange={handleOrderInputChange}
                required
              >
                <option value="">Müşteri seçin</option>
                {customers.map((customer) => (
                  <option key={customer.id} value={customer.id}>
                    {customer.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Ürün
              <select
                name="productId"
                value={orderForm.productId}
                onChange={handleOrderInputChange}
                required
              >
                <option value="">Ürün seçin</option>
                {selectableProducts.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Miktar
              <input
                name="quantity"
                type="number"
                min="1"
                step="1"
                value={orderForm.quantity}
                onChange={handleOrderInputChange}
                required
              />
            </label>

            <button type="submit" className="primary-button" disabled={submittingOrder}>
              {submittingOrder ? 'Oluşturuluyor...' : 'Sipariş Oluştur'}
            </button>
          </form>

          <ul className="order-list">
            {orders.slice(0, 5).map((order) => (
              <li key={order.id}>
                <div>
                  <strong>{order.customerName}</strong>
                  <small>{orderStatusLabels[order.status] ?? 'Bilinmiyor'}</small>
                </div>
                <div className="order-actions">
                  <span>{formatMoney(order.totalAmount)}</span>
                  {order.status === 1 && (
                    <>
                      <button
                        type="button"
                        className="small-button"
                        onClick={() => void updateOrderStatus(order.id, 'confirm')}
                      >
                        Onayla
                      </button>
                      <button
                        type="button"
                        className="small-button secondary"
                        onClick={() => void updateOrderStatus(order.id, 'cancel')}
                      >
                        İptal
                      </button>
                    </>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className="panel movement-panel">
        <div className="panel-header">
          <h2>Stok Hareketleri</h2>
          <span>{stockMovements.length} kayıt</span>
        </div>

        <form className="movement-form" onSubmit={handleMovementSubmit}>
          <label>
            Ürün
            <select
              name="productId"
              value={movementForm.productId}
              onChange={handleMovementInputChange}
              required
            >
              <option value="">Ürün seçin</option>
              {selectableProducts.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name} ({product.stockQuantity} adet)
                </option>
              ))}
            </select>
          </label>

          <label>
            Hareket
            <select
              name="type"
              value={movementForm.type}
              onChange={handleMovementInputChange}
            >
              <option value="1">Stok girişi</option>
              <option value="2">Stok çıkışı</option>
            </select>
          </label>

          <label>
            Miktar
            <input
              name="quantity"
              type="number"
              min="1"
              step="1"
              value={movementForm.quantity}
              onChange={handleMovementInputChange}
              required
            />
          </label>

          <label>
            Açıklama
            <input
              name="description"
              value={movementForm.description}
              onChange={handleMovementInputChange}
              maxLength={500}
              placeholder="İsteğe bağlı"
            />
          </label>

          <button type="submit" className="primary-button" disabled={submittingMovement}>
            {submittingMovement ? 'Kaydediliyor...' : 'Hareket Kaydet'}
          </button>
        </form>

        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Tür</th>
                <th>Miktar</th>
                <th>Açıklama</th>
                <th>Tarih</th>
              </tr>
            </thead>
            <tbody>
              {stockMovements.slice(0, 10).map((movement) => (
                <tr key={movement.id}>
                  <td>{movement.productName}</td>
                  <td>{movement.type === 1 ? 'Giriş' : 'Çıkış'}</td>
                  <td>{movement.quantity}</td>
                  <td>{movement.description ?? '—'}</td>
                  <td>{new Intl.DateTimeFormat('tr-TR').format(new Date(movement.createdAtUtc))}</td>
                </tr>
              ))}
              {stockMovements.length === 0 && (
                <tr>
                  <td colSpan={5} className="empty-state">Henüz stok hareketi yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

export default App
