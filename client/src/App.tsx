import { useEffect, useEffectEvent, useState } from 'react'
import {
  ArrowLeftRight,
  Boxes,
  ChevronLeft,
  ChevronRight,
  LayoutDashboard,
  LogOut,
  Package,
  RefreshCw,
  ShoppingCart,
  Tags,
  Users,
} from 'lucide-react'
import './App.css'

type AppView = 'overview' | 'products' | 'customers' | 'categories' | 'orders' | 'stock'

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

type DraftOrderItem = {
  productId: string
  quantity: number
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

type CategoryFormState = {
  name: string
  description: string
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

type AuthResponse = {
  accessToken: string
  expiresAtUtc: string
  email: string
  roles: string[]
}

type LoginFormState = {
  email: string
  password: string
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

const emptyCategoryForm: CategoryFormState = {
  name: '',
  description: '',
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

const emptyLoginForm: LoginFormState = {
  email: '',
  password: '',
}

const orderStatusLabels: Record<number, string> = {
  1: 'Bekliyor',
  2: 'Onaylandı',
  3: 'İptal edildi',
}

const viewTitles: Record<AppView, string> = {
  overview: 'Genel Bakış',
  products: 'Ürünler',
  customers: 'Müşteriler',
  categories: 'Kategoriler',
  orders: 'Siparişler',
  stock: 'Stok Hareketleri',
}

const formatMoney = (value: number) =>
  new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    maximumFractionDigits: 2,
  }).format(value)

function App() {
  const [activeView, setActiveView] = useState<AppView>('overview')
  const [accessToken, setAccessToken] = useState<string | null>(
    () => sessionStorage.getItem('productflow.accessToken'),
  )
  const [userEmail, setUserEmail] = useState(
    () => sessionStorage.getItem('productflow.email') ?? '',
  )
  const [userRoles, setUserRoles] = useState<string[]>(() => {
    try {
      return JSON.parse(sessionStorage.getItem('productflow.roles') ?? '[]') as string[]
    } catch {
      return []
    }
  })
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
  const [submittingCategory, setSubmittingCategory] = useState(false)
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null)
  const [submittingCustomer, setSubmittingCustomer] = useState(false)
  const [editingCustomerId, setEditingCustomerId] = useState<string | null>(null)
  const [submittingOrder, setSubmittingOrder] = useState(false)
  const [orderItems, setOrderItems] = useState<DraftOrderItem[]>([])
  const [submittingMovement, setSubmittingMovement] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [loginForm, setLoginForm] = useState<LoginFormState>(emptyLoginForm)
  const [submittingLogin, setSubmittingLogin] = useState(false)
  const [productSearch, setProductSearch] = useState('')
  const [productSearchResultCount, setProductSearchResultCount] = useState(0)
  const [productPage, setProductPage] = useState(1)
  const [productTotalPages, setProductTotalPages] = useState(1)
  const [productCategoryFilter, setProductCategoryFilter] = useState('')
  const [productSortBy, setProductSortBy] = useState('name')
  const [productSortOrder, setProductSortOrder] = useState('asc')
  const [orderStatusFilter, setOrderStatusFilter] = useState('')
  const [stockProductFilter, setStockProductFilter] = useState('')
  const [stockTypeFilter, setStockTypeFilter] = useState('')
  const [productForm, setProductForm] = useState<ProductFormState>(emptyProductForm)
  const [categoryForm, setCategoryForm] = useState<CategoryFormState>(emptyCategoryForm)
  const [customerForm, setCustomerForm] = useState<CustomerFormState>(emptyCustomerForm)
  const [orderForm, setOrderForm] = useState<OrderFormState>(emptyOrderForm)
  const [movementForm, setMovementForm] =
    useState<StockMovementFormState>(emptyStockMovementForm)

  const apiFetch = (
    input: RequestInfo | URL,
    init: RequestInit = {},
    token = accessToken,
  ) => {
    const headers = new Headers(init.headers)
    if (token) headers.set('Authorization', `Bearer ${token}`)
    return fetch(input, { ...init, headers }).then((response) => {
      if (response.status === 401 && token) {
        sessionStorage.removeItem('productflow.accessToken')
        sessionStorage.removeItem('productflow.email')
        sessionStorage.removeItem('productflow.roles')
        setAccessToken(null)
        setUserEmail('')
        setUserRoles([])
        setMessage('Oturum süresi doldu. Yeniden giriş yapın.')
      }
      return response
    })
  }

  const fetchDashboard = async (
    searchTerm = productSearch,
    token = accessToken,
    page = productPage,
    categoryId = productCategoryFilter,
  ) => {
    if (!token) return
    try {
      const productQuery = new URLSearchParams({
        page: String(page),
        pageSize: '10',
        sortBy: productSortBy,
        sortOrder: productSortOrder,
      })
      if (searchTerm) productQuery.set('search', searchTerm)
      if (categoryId) productQuery.set('categoryId', categoryId)
      const [
        productsResponse,
        allProductsResponse,
        criticalProductsResponse,
        categoriesResponse,
        customersResponse,
        ordersResponse,
        movementsResponse,
      ] = await Promise.all([
          apiFetch(`/api/products?${productQuery}`, {}, token),
          apiFetch('/api/products?page=1&pageSize=100', {}, token),
          apiFetch('/api/products/critical-stock', {}, token),
          apiFetch('/api/categories', {}, token),
          apiFetch('/api/customers', {}, token),
          apiFetch('/api/orders', {}, token),
          apiFetch('/api/stockmovements', {}, token),
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
      const remainingProductPages = await Promise.all(
        Array.from({ length: Math.max(0, allProductsData.totalPages - 1) }, (_, index) =>
          apiFetch(`/api/products?page=${index + 2}&pageSize=100`, {}, token),
        ),
      )
      if (remainingProductPages.some((response) => !response.ok)) {
        throw new Error('Ürün seçenekleri alınamadı.')
      }
      const remainingProductData = await Promise.all(
        remainingProductPages.map((response) => response.json() as Promise<PagedProducts>),
      )
      const allSelectableProducts = [
        ...(allProductsData.items ?? []),
        ...remainingProductData.flatMap((page) => page.items ?? []),
      ]
      const criticalProductsData = (await criticalProductsResponse.json()) as Product[]
      const categoriesData = (await categoriesResponse.json()) as Category[]
      const customersData = (await customersResponse.json()) as Customer[]
      const ordersData = (await ordersResponse.json()) as Order[]
      const movementsData = (await movementsResponse.json()) as StockMovement[]

      setProducts(productsData.items ?? [])
      setProductSearchResultCount(productsData.totalCount ?? 0)
      setProductPage(productsData.page ?? page)
      setProductTotalPages(productsData.totalPages ?? 1)
      setSelectableProducts(allSelectableProducts)
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

      if (allSelectableProducts.length > 0 && !movementForm.productId) {
        setMovementForm((current) => ({
          ...current,
          productId: allSelectableProducts[0].id,
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

  const loadDashboardEffect = useEffectEvent(() => {
    if (accessToken) void fetchDashboard()
  })

  useEffect(() => {
    void Promise.resolve().then(loadDashboardEffect)
  }, [accessToken])

  const handleLoginInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target
    setLoginForm((current) => ({ ...current, [name]: value }))
  }

  const handleLoginSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    try {
      setSubmittingLogin(true)
      setMessage(null)
      const response = await apiFetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(loginForm),
      }, null)

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Giriş başarısız.')
      }

      const result = (await response.json()) as AuthResponse
      sessionStorage.setItem('productflow.accessToken', result.accessToken)
      sessionStorage.setItem('productflow.email', result.email)
      sessionStorage.setItem('productflow.roles', JSON.stringify(result.roles))
      setAccessToken(result.accessToken)
      setUserEmail(result.email)
      setUserRoles(result.roles)
      setLoginForm(emptyLoginForm)
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Giriş yapılamadı.')
    } finally {
      setSubmittingLogin(false)
    }
  }

  const handleLogout = () => {
    sessionStorage.removeItem('productflow.accessToken')
    sessionStorage.removeItem('productflow.email')
    sessionStorage.removeItem('productflow.roles')
    setAccessToken(null)
    setUserEmail('')
    setUserRoles([])
    setMessage(null)
  }

  const canManage = userRoles.some((role) => role === 'Admin' || role === 'Manager')
  const isAdmin = userRoles.includes('Admin')

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

  const handleCategoryInputChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = event.target
    setCategoryForm((current) => ({ ...current, [name]: value }))
  }

  const handleOrderInputChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = event.target
    setOrderForm((current) => ({ ...current, [name]: value }))
  }

  const addOrderItem = () => {
    const quantity = Number(orderForm.quantity)
    const product = selectableProducts.find(
      (item) => item.id === orderForm.productId,
    )

    if (!product) {
      setMessage('Sipariş kalemi için ürün seçmelisiniz.')
      return
    }

    if (!Number.isInteger(quantity) || quantity < 1) {
      setMessage('Ürün miktarı en az 1 olmalıdır.')
      return
    }

    const existingItem = orderItems.find((item) => item.productId === product.id)
    const combinedQuantity = quantity + (existingItem?.quantity ?? 0)
    if (combinedQuantity > product.stockQuantity) {
      setMessage(`${product.name} için stok yetersiz. Mevcut stok: ${product.stockQuantity}.`)
      return
    }

    setOrderItems((current) => {
      const existing = current.find((item) => item.productId === product.id)
      if (existing) {
        return current.map((item) =>
          item.productId === product.id
            ? { ...item, quantity: item.quantity + quantity }
            : item,
        )
      }
      return [...current, { productId: product.id, quantity }]
    })
    setOrderForm((current) => ({ ...current, productId: '', quantity: '1' }))
    setMessage(null)
  }

  const removeOrderItem = (productId: string) => {
    setOrderItems((current) => current.filter((item) => item.productId !== productId))
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

      const response = await apiFetch(
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
    setLoading(true)
    void fetchDashboard(productSearch.trim(), accessToken, 1)
  }

  const changeProductPage = (page: number) => {
    setLoading(true)
    void fetchDashboard(productSearch, accessToken, page)
  }

  const visibleOrders = orders.filter((order) =>
    !orderStatusFilter || String(order.status) === orderStatusFilter,
  )

  const visibleStockMovements = stockMovements.filter((movement) =>
    (!stockProductFilter || movement.productId === stockProductFilter) &&
    (!stockTypeFilter || String(movement.type) === stockTypeFilter),
  )

  const handleCustomerSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    try {
      setSubmittingCustomer(true)
      setMessage(null)

      const response = await apiFetch(
        editingCustomerId ? `/api/customers/${editingCustomerId}` : '/api/customers',
        {
        method: editingCustomerId ? 'PUT' : 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(customerForm),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(
          errorPayload?.message ?? 'Müşteri kaydedilemedi.',
        )
      }

      setCustomerForm(emptyCustomerForm)
      setEditingCustomerId(null)
      setMessage(editingCustomerId ? 'Müşteri güncellendi.' : 'Müşteri başarıyla eklendi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(
        err instanceof Error ? err.message : 'Müşteri kaydedilemedi.',
      )
    } finally {
      setSubmittingCustomer(false)
    }
  }

  const beginCustomerEdit = (customer: Customer) => {
    setEditingCustomerId(customer.id)
    setCustomerForm({
      name: customer.name,
      email: customer.email,
      phone: customer.phone ?? '',
    })
    setMessage(null)
  }

  const cancelCustomerEdit = () => {
    setEditingCustomerId(null)
    setCustomerForm(emptyCustomerForm)
  }

  const deactivateCustomer = async (customer: Customer) => {
    if (!window.confirm(`${customer.name} müşterisini pasife almak istiyor musunuz?`)) {
      return
    }

    try {
      setMessage(null)
      const response = await apiFetch(`/api/customers/${customer.id}/deactivate`, {
        method: 'PATCH',
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Müşteri pasife alınamadı.')
      }

      if (editingCustomerId === customer.id) {
        cancelCustomerEdit()
      }
      setMessage('Müşteri pasife alındı.')
      await fetchDashboard()
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Müşteri pasife alınamadı.')
    }
  }

  const handleCategorySubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    try {
      setSubmittingCategory(true)
      setMessage(null)
      const response = await apiFetch(
        editingCategoryId
          ? `/api/categories/${editingCategoryId}`
          : '/api/categories',
        {
          method: editingCategoryId ? 'PUT' : 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(categoryForm),
        },
      )

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Kategori kaydedilemedi.')
      }

      setCategoryForm(emptyCategoryForm)
      setEditingCategoryId(null)
      setMessage(editingCategoryId ? 'Kategori güncellendi.' : 'Kategori eklendi.')
      await fetchDashboard()
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Kategori kaydedilemedi.')
    } finally {
      setSubmittingCategory(false)
    }
  }

  const beginCategoryEdit = (category: Category) => {
    setEditingCategoryId(category.id)
    setCategoryForm({
      name: category.name,
      description: category.description ?? '',
    })
    setMessage(null)
  }

  const cancelCategoryEdit = () => {
    setEditingCategoryId(null)
    setCategoryForm(emptyCategoryForm)
  }

  const deactivateCategory = async (category: Category) => {
    try {
      setMessage(null)
      const response = await apiFetch(`/api/categories/${category.id}/deactivate`, {
        method: 'PATCH',
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Kategori pasife alınamadı.')
      }

      if (editingCategoryId === category.id) {
        cancelCategoryEdit()
      }
      if (productForm.categoryId === category.id) {
        const nextCategory = categories.find(
          (item) => item.id !== category.id && item.isActive !== false,
        )
        setProductForm((current) => ({
          ...current,
          categoryId: nextCategory?.id ?? '',
        }))
      }

      setMessage('Kategori pasife alındı.')
      await fetchDashboard()
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Kategori pasife alınamadı.')
    }
  }

  const handleOrderSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!orderForm.customerId || orderItems.length === 0) {
      setMessage('Müşteri seçin ve siparişe en az bir kalem ekleyin.')
      return
    }

    try {
      setSubmittingOrder(true)
      setMessage(null)

      const response = await apiFetch('/api/orders', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          customerId: orderForm.customerId,
          items: orderItems,
        }),
      })

      if (!response.ok) {
        const errorPayload = await response.json().catch(() => null)
        throw new Error(errorPayload?.message ?? 'Sipariş oluşturulamadı.')
      }

      setOrderForm(emptyOrderForm)
      setOrderItems([])
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
      const response = await apiFetch(`/api/orders/${orderId}/${action}`, {
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
      const response = await apiFetch('/api/stockmovements', {
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

  const navigationItems = [
    { id: 'overview', label: 'Genel Bakış', icon: LayoutDashboard },
    { id: 'products', label: 'Ürünler', icon: Package },
    { id: 'customers', label: 'Müşteriler', icon: Users },
    { id: 'categories', label: 'Kategoriler', icon: Tags },
    { id: 'orders', label: 'Siparişler', icon: ShoppingCart },
    { id: 'stock', label: 'Stok Hareketleri', icon: ArrowLeftRight },
  ] as const

  if (!accessToken) {
    return (
      <main className="login-shell">
        <section className="login-panel">
          <div className="brand-lockup login-brand">
            <span className="brand-mark"><Boxes size={21} strokeWidth={2.2} /></span>
            <span className="brand-copy">
              <strong>ProductFlow</strong>
              <small>INVENTORY STUDIO</small>
            </span>
          </div>
          <p className="panel-kicker">OPERASYON YÖNETİMİ</p>
          <h1>Hesabınıza giriş yapın</h1>
          <p className="login-description">Envanter ve sipariş çalışma alanınıza devam edin.</p>
          {message && <div className="alert" role="alert">{message}</div>}
          <form className="product-form" onSubmit={handleLoginSubmit}>
            <label>
              E-posta
              <input
                name="email"
                type="email"
                autoComplete="username"
                value={loginForm.email}
                onChange={handleLoginInputChange}
                required
              />
            </label>
            <label>
              Parola
              <input
                name="password"
                type="password"
                autoComplete="current-password"
                value={loginForm.password}
                onChange={handleLoginInputChange}
                required
              />
            </label>
            <button type="submit" className="primary-button" disabled={submittingLogin}>
              {submittingLogin ? 'Giriş yapılıyor...' : 'Giriş Yap'}
            </button>
          </form>
        </section>
      </main>
    )
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-lockup">
          <span className="brand-mark"><Boxes size={21} strokeWidth={2.2} /></span>
          <span className="brand-copy">
            <strong>ProductFlow</strong>
            <small>INVENTORY STUDIO</small>
          </span>
        </div>

        <div className="nav-caption">ÇALIŞMA ALANI</div>
        <nav className="primary-nav" aria-label="Ana navigasyon">
          {navigationItems.map((item) => {
            const Icon = item.icon
            return (
              <button
                key={item.id}
                type="button"
                className={activeView === item.id ? 'nav-item active' : 'nav-item'}
                onClick={() => setActiveView(item.id)}
                aria-current={activeView === item.id ? 'page' : undefined}
                title={item.label}
              >
                <Icon size={18} strokeWidth={1.9} />
                <span>{item.label}</span>
                {activeView === item.id && <ChevronRight className="nav-chevron" size={15} />}
              </button>
            )
          })}
        </nav>

        <div className="sidebar-footer">
          <span className="status-indicator" />
          <span className="signed-in-user">{userEmail}</span>
          <button type="button" className="logout-button" onClick={handleLogout} title="Çıkış yap" aria-label="Çıkış yap">
            <LogOut size={16} />
          </button>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div className="page-title-group">
            <p className="eyebrow">PRODUCTFLOW <span>/</span> ENVANTER</p>
            <h1>{viewTitles[activeView]}</h1>
          </div>
          <button
            type="button"
            className="icon-button"
            onClick={() => {
              setLoading(true)
              void fetchDashboard()
            }}
            aria-label="Verileri yenile"
            title="Verileri yenile"
          >
            <RefreshCw size={17} />
          </button>
        </header>

        {message && <div className="alert" role="status">{message}</div>}

        {activeView === 'overview' && (
          <div className="view-content">
            <section className="stats-grid">
              <article className="stat-card stat-products">
                <span>Toplam ürün</span>
                <strong>{totalProductCount}</strong>
                <Package size={19} />
              </article>
              <article className="stat-card stat-critical">
                <span>Kritik stok</span>
                <strong>{criticalProductCount}</strong>
                <span className="stat-note">Takip gerekli</span>
              </article>
              <article className="stat-card stat-customers">
                <span>Aktif müşteri</span>
                <strong>{customers.filter((customer) => customer.isActive !== false).length}</strong>
                <Users size={19} />
              </article>
              <article className="stat-card stat-orders">
                <span>Sipariş</span>
                <strong>{orders.length}</strong>
                <ShoppingCart size={19} />
              </article>
            </section>

            <section className="overview-grid">
              <div className="panel overview-panel">
                <div className="panel-header">
                  <div>
                    <p className="panel-kicker">ENVANTER</p>
                    <h2>Kritik stoktakiler</h2>
                  </div>
                  <button type="button" className="text-button" onClick={() => setActiveView('products')}>
                    Ürünlere git <ChevronRight size={15} />
                  </button>
                </div>
                {selectableProducts.filter((product) => product.stockQuantity <= (product.minimumStockLevel ?? 0)).length === 0 ? (
                  <p className="empty-state">Kritik seviyede ürün yok.</p>
                ) : (
                  <ul className="overview-stock-list">
                    {selectableProducts
                      .filter((product) => product.stockQuantity <= (product.minimumStockLevel ?? 0))
                      .slice(0, 5)
                      .map((product) => (
                        <li key={product.id}>
                          <div className="overview-product-icon"><Package size={17} /></div>
                          <div className="overview-product-copy">
                            <strong>{product.name}</strong>
                            <small>{product.code} · Min. {product.minimumStockLevel ?? 0}</small>
                          </div>
                          <span className="stock-badge danger">{product.stockQuantity}</span>
                        </li>
                      ))}
                  </ul>
                )}
              </div>

              <div className="panel overview-panel">
                <div className="panel-header">
                  <div>
                    <p className="panel-kicker">SON İŞLEMLER</p>
                    <h2>Yeni siparişler</h2>
                  </div>
                  <button type="button" className="text-button" onClick={() => setActiveView('orders')}>
                    Tüm siparişler <ChevronRight size={15} />
                  </button>
                </div>
                {orders.length === 0 ? (
                  <p className="empty-state">Henüz sipariş yok.</p>
                ) : (
                  <ul className="overview-order-list">
                    {orders.slice(0, 5).map((order) => (
                      <li key={order.id}>
                        <div className="order-avatar">{order.customerName.slice(0, 1).toLocaleUpperCase('tr-TR')}</div>
                        <div className="overview-product-copy">
                          <strong>{order.customerName}</strong>
                          <small>{new Intl.DateTimeFormat('tr-TR').format(new Date(order.createdAtUtc))}</small>
                        </div>
                        <div className="overview-order-total">
                          <strong>{formatMoney(order.totalAmount)}</strong>
                          <small>{orderStatusLabels[order.status] ?? 'Bilinmiyor'}</small>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </section>
          </div>
        )}

        {activeView === 'products' && <section className="content-grid view-content">
        <div className="panel">
          <div className="panel-header">
            <h2>Ürünler</h2>
            <span>
              {productSearch
                ? `${productSearchResultCount} eşleşme`
                : `${totalProductCount} kayıt`}
            </span>
          </div>

          <form className="search-form product-filters" onSubmit={handleProductSearch}>
            <input
              type="search"
              value={productSearch}
              onChange={(event) => setProductSearch(event.target.value)}
              placeholder="Ürün adı veya kodu ara"
              aria-label="Ürün adı veya kodu ara"
            />
            <select
              aria-label="Kategori filtresi"
              value={productCategoryFilter}
              onChange={(event) => setProductCategoryFilter(event.target.value)}
            >
              <option value="">Tüm kategoriler</option>
              {categories.filter((category) => category.isActive !== false).map((category) => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
            <select
              aria-label="Ürün sıralaması"
              value={productSortBy}
              onChange={(event) => setProductSortBy(event.target.value)}
            >
              <option value="name">Ada göre</option>
              <option value="price">Fiyata göre</option>
              <option value="stockQuantity">Stoğa göre</option>
            </select>
            <select
              aria-label="Sıralama yönü"
              value={productSortOrder}
              onChange={(event) => setProductSortOrder(event.target.value)}
            >
              <option value="asc">Artan</option>
              <option value="desc">Azalan</option>
            </select>
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
                        {canManage && (
                        <button
                          type="button"
                          className="small-button"
                          onClick={() => beginProductEdit(product)}
                        >
                          Düzenle
                        </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="pagination-bar">
            <span>{productPage} / {productTotalPages} sayfa</span>
            <div>
              <button
                type="button"
                className="icon-button pagination-button"
                aria-label="Önceki sayfa"
                disabled={productPage <= 1 || loading}
                onClick={() => changeProductPage(productPage - 1)}
              >
                <ChevronLeft size={16} />
              </button>
              <button
                type="button"
                className="icon-button pagination-button"
                aria-label="Sonraki sayfa"
                disabled={productPage >= productTotalPages || loading}
                onClick={() => changeProductPage(productPage + 1)}
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        </div>

        {canManage && <aside className="panel side-panel">
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
                  {categories.filter((category) => category.isActive !== false).map((category) => (
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
        </aside>}
      </section>}

      {(activeView === 'customers' || activeView === 'categories' || activeView === 'orders') && (
      <section className="module-grid view-content">
        {activeView === 'customers' && <div className="panel">
          <div className="panel-header">
            <h2>{editingCustomerId ? 'Müşteriyi Düzenle' : 'Müşteriler'}</h2>
          </div>

          {canManage && <form className="product-form" onSubmit={handleCustomerSubmit}>
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
              {submittingCustomer
                ? 'Kaydediliyor...'
                : editingCustomerId
                  ? 'Değişiklikleri Kaydet'
                  : 'Müşteri Ekle'}
            </button>
            {editingCustomerId && (
              <button
                type="button"
                className="small-button secondary"
                onClick={cancelCustomerEdit}
              >
                Düzenlemeyi İptal Et
              </button>
            )}
          </form>}

          <ul className="info-list">
            {customers.map((customer) => (
              <li key={customer.id}>
                <strong>{customer.name}</strong>
                <span>{customer.email}</span>
                <small className={customer.isActive === false ? 'inactive-label' : 'active-label'}>
                  {customer.isActive === false ? 'Pasif' : 'Aktif'}
                </small>
                {canManage && <div className="customer-actions">
                  <button
                    type="button"
                    className="small-button"
                    onClick={() => beginCustomerEdit(customer)}
                  >
                    Düzenle
                  </button>
                  {isAdmin && customer.isActive !== false && (
                    <button
                      type="button"
                      className="small-button secondary"
                      onClick={() => void deactivateCustomer(customer)}
                    >
                      Pasife al
                    </button>
                  )}
                </div>}
              </li>
            ))}
          </ul>
        </div>}

        {activeView === 'categories' && <div className="panel">
          <div className="panel-header">
            <h2>{editingCategoryId ? 'Kategoriyi Düzenle' : 'Kategoriler'}</h2>
          </div>

          {canManage && <form className="product-form" onSubmit={handleCategorySubmit}>
            <label>
              Kategori adı
              <input
                name="name"
                value={categoryForm.name}
                onChange={handleCategoryInputChange}
                minLength={2}
                maxLength={100}
                required
              />
            </label>

            <label>
              Açıklama
              <textarea
                name="description"
                value={categoryForm.description}
                onChange={handleCategoryInputChange}
                maxLength={500}
                rows={2}
              />
            </label>

            <button type="submit" className="primary-button" disabled={submittingCategory}>
              {submittingCategory
                ? 'Kaydediliyor...'
                : editingCategoryId
                  ? 'Değişiklikleri Kaydet'
                  : 'Kategori Ekle'}
            </button>
            {editingCategoryId && (
              <button
                type="button"
                className="small-button secondary"
                onClick={cancelCategoryEdit}
              >
                Düzenlemeyi İptal Et
              </button>
            )}
          </form>}

          <ul className="category-management-list">
            {categories.map((category) => (
              <li key={category.id}>
                <div className="category-details">
                  <strong>{category.name}</strong>
                  <small>{category.description || 'Açıklama yok'}</small>
                  <span className={category.isActive === false ? 'inactive-label' : 'active-label'}>
                    {category.isActive === false ? 'Pasif' : 'Aktif'}
                  </span>
                </div>
                {canManage && <div className="category-actions">
                  <button
                    type="button"
                    className="small-button"
                    onClick={() => beginCategoryEdit(category)}
                  >
                    Düzenle
                  </button>
                  {isAdmin && category.isActive !== false && (
                    <button
                      type="button"
                      className="small-button secondary"
                      onClick={() => void deactivateCategory(category)}
                    >
                      Pasife al
                    </button>
                  )}
                </div>}
              </li>
            ))}
          </ul>
        </div>}

        {activeView === 'orders' && <div className="panel">
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
                {customers.filter((customer) => customer.isActive !== false).map((customer) => (
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
              >
                <option value="">Ürün seçin</option>
                {selectableProducts
                  .filter((product) => product.isActive !== false)
                  .map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name} · {product.stockQuantity} adet · {formatMoney(product.price)}
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

            <button type="button" className="small-button" onClick={addOrderItem}>
              Kalemi Ekle
            </button>

            <ul className="order-draft-list">
              {orderItems.map((item) => {
                const product = selectableProducts.find(
                  (candidate) => candidate.id === item.productId,
                )
                if (!product) return null

                return (
                  <li key={item.productId}>
                    <div>
                      <strong>{product.name}</strong>
                      <small>{item.quantity} × {formatMoney(product.price)}</small>
                    </div>
                    <span>{formatMoney(item.quantity * product.price)}</span>
                    <button
                      type="button"
                      className="small-button secondary"
                      onClick={() => removeOrderItem(item.productId)}
                      aria-label={`${product.name} kalemini kaldır`}
                    >
                      Kaldır
                    </button>
                  </li>
                )
              })}
              {orderItems.length === 0 && (
                <li className="order-draft-empty">Henüz sipariş kalemi eklenmedi.</li>
              )}
            </ul>

            <div className="order-total">
              <span>Tahmini toplam</span>
              <strong>
                {formatMoney(
                  orderItems.reduce((total, item) => {
                    const product = selectableProducts.find(
                      (candidate) => candidate.id === item.productId,
                    )
                    return total + item.quantity * (product?.price ?? 0)
                  }, 0),
                )}
              </strong>
            </div>

            <button
              type="submit"
              className="primary-button"
              disabled={submittingOrder || orderItems.length === 0}
            >
              {submittingOrder ? 'Oluşturuluyor...' : 'Siparişi Oluştur'}
            </button>
          </form>

          <div className="list-filter-row">
            <label htmlFor="order-status-filter">Durum</label>
            <select
              id="order-status-filter"
              value={orderStatusFilter}
              onChange={(event) => setOrderStatusFilter(event.target.value)}
            >
              <option value="">Tüm durumlar</option>
              <option value="1">Bekliyor</option>
              <option value="2">Onaylandı</option>
              <option value="3">İptal edildi</option>
            </select>
            <span>{visibleOrders.length} sipariş</span>
          </div>
          <ul className="order-list">
            {visibleOrders.map((order) => (
              <li key={order.id}>
                <div>
                  <strong>{order.customerName}</strong>
                  <small>{orderStatusLabels[order.status] ?? 'Bilinmiyor'}</small>
                </div>
                <div className="order-actions">
                  <span>{formatMoney(order.totalAmount)}</span>
                  {order.status === 1 && canManage && (
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
        </div>}
      </section>
      )}

      {activeView === 'stock' && <section className="panel movement-panel view-content">
        <div className="panel-header">
          <h2>Stok Hareketleri</h2>
          <span>{visibleStockMovements.length} kayıt</span>
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

        <div className="stock-filters">
          <label>
            Ürün
            <select
              value={stockProductFilter}
              onChange={(event) => setStockProductFilter(event.target.value)}
            >
              <option value="">Tüm ürünler</option>
              {selectableProducts.map((product) => (
                <option key={product.id} value={product.id}>{product.name}</option>
              ))}
            </select>
          </label>
          <label>
            Hareket türü
            <select
              value={stockTypeFilter}
              onChange={(event) => setStockTypeFilter(event.target.value)}
            >
              <option value="">Tümü</option>
              <option value="1">Giriş</option>
              <option value="2">Çıkış</option>
            </select>
          </label>
        </div>

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
              {visibleStockMovements.map((movement) => (
                <tr key={movement.id}>
                  <td>{movement.productName}</td>
                  <td>{movement.type === 1 ? 'Giriş' : 'Çıkış'}</td>
                  <td>{movement.quantity}</td>
                  <td>{movement.description ?? '—'}</td>
                  <td>{new Intl.DateTimeFormat('tr-TR').format(new Date(movement.createdAtUtc))}</td>
                </tr>
              ))}
              {visibleStockMovements.length === 0 && (
                <tr>
                  <td colSpan={5} className="empty-state">Henüz stok hareketi yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>}
      </main>
    </div>
  )
}

export default App
