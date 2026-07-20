// Add to Cart with Confirmation Modal
let currentProductId = null;
let currentProductPrice = 0;
let buyNowMode = false;

document.addEventListener("DOMContentLoaded", function () {
  document.addEventListener("click", function (e) {
    const link = e.target.closest('.btn-add-cart[href*="AddToCart"]');
    if (link) {
      e.preventDefault();
      buyNowMode = link.dataset.buyNow === "true";
      updateModalButtons();
      openAddToCartModal(link.href);
    }
  });

  const decreaseBtn = document.getElementById("decreaseQty");
  const increaseBtn = document.getElementById("increaseQty");
  const qtyInput = document.getElementById("modalQuantity");

  if (decreaseBtn) {
    decreaseBtn.addEventListener("click", function () {
      const qty = Math.max(1, parseInt(qtyInput.value) - 1);
      qtyInput.value = qty;
      updateModalTotalPrice();
    });
  }

  if (increaseBtn) {
    increaseBtn.addEventListener("click", function () {
      const qty = parseInt(qtyInput.value) + 1;
      qtyInput.value = qty;
      updateModalTotalPrice();
    });
  }

  if (qtyInput) {
    qtyInput.addEventListener("change", function () {
      this.value = Math.max(1, parseInt(this.value) || 1);
      updateModalTotalPrice();
    });
    qtyInput.addEventListener("input", updateModalTotalPrice);
  }

  const confirmBtn = document.getElementById("confirmAddToCart");
  if (confirmBtn) {
    confirmBtn.addEventListener("click", function () {
      const qty = parseInt(document.getElementById("modalQuantity").value) || 1;
      confirmAddToCart(currentProductId, qty);
    });
  }

  const buyNowBtn = document.getElementById("confirmBuyNow");
  if (buyNowBtn) {
    buyNowBtn.addEventListener("click", function () {
      const qty = parseInt(document.getElementById("modalQuantity").value) || 1;
      confirmAddToCart(currentProductId, qty, "/ShoppingCart/Checkout");
    });
  }

  // Initialize Swiper Carousel for Products on Homepage
  if (typeof Swiper !== 'undefined' && document.querySelector('.products-swiper')) {
    new Swiper('.products-swiper', {
      slidesPerView: 1.25,
      spaceBetween: 16,
      loop: true,
      centeredSlides: false,
      autoplay: {
        delay: 3500,
        disableOnInteraction: false,
        pauseOnMouseEnter: true,
      },
      pagination: {
        el: '.swiper-pagination',
        clickable: true,
        dynamicBullets: true,
      },
      navigation: {
        nextEl: '.swiper-button-next',
        prevEl: '.swiper-button-prev',
      },
      breakpoints: {
        576: {
          slidesPerView: 2,
          spaceBetween: 20
        },
        768: {
          slidesPerView: 3,
          spaceBetween: 24
        },
        992: {
          slidesPerView: 4,
          spaceBetween: 24
        }
      }
    });
  }
});

function updateModalButtons() {
  const buyNowBtn = document.getElementById("confirmBuyNow");
  const modalTitle = document.getElementById("addToCartLabel");
  if (buyNowBtn) {
    buyNowBtn.classList.toggle("d-none", !buyNowMode);
  }
  if (modalTitle) {
    modalTitle.textContent = buyNowMode ? "Mua ngay" : "Thêm vào giỏ hàng";
  }
}

function openAddToCartModal(url) {
  const productId = new URL(url, window.location.origin).searchParams.get("productId");
  if (!productId) {
    ToastNotification.error("Lỗi", "Không tìm thấy sản phẩm");
    return;
  }

  fetch(`/api/products/${productId}`)
    .then((response) => response.json())
    .then((product) => {
      currentProductId = productId;
      currentProductPrice = product.price;

      document.getElementById("modalProductImage").src = product.imageUrl || "";
      document.getElementById("modalProductName").textContent = product.name;
      document.getElementById("modalProductCategory").textContent = product.category || "";
      document.getElementById("modalProductPrice").textContent =
        product.price.toLocaleString("vi-VN") + "đ";
      document.getElementById("modalQuantity").value = 1;

      updateModalTotalPrice();
      updateModalButtons();

      const modal = new bootstrap.Modal(document.getElementById("addToCartModal"));
      modal.show();
    })
    .catch((error) => {
      ToastNotification.error("Lỗi", "Không thể tải thông tin sản phẩm");
      console.error("Error:", error);
    });
}

function updateModalTotalPrice() {
  const qty = parseInt(document.getElementById("modalQuantity").value) || 1;
  const total = currentProductPrice * qty;
  document.getElementById("modalTotalPrice").textContent =
    total.toLocaleString("vi-VN") + "đ";
}

function confirmAddToCart(productId, quantity, redirectUrl = null) {
  const url = `/ShoppingCart/AddToCart?productId=${productId}&quantity=${quantity}`;

  fetch(url, {
    method: "GET",
    headers: { "X-Requested-With": "XMLHttpRequest" },
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        const modal = bootstrap.Modal.getInstance(document.getElementById("addToCartModal"));
        if (modal) modal.hide();

        if (redirectUrl) {
          window.location.href = redirectUrl;
          return;
        }

        ToastNotification.success("Thành công", data.message);
        updateCartBadge(data.cartCount);
        openMiniCart();
      } else {
        ToastNotification.error("Lỗi", "Không thể thêm sản phẩm vào giỏ hàng");
      }
    })
    .catch((error) => {
      ToastNotification.error("Lỗi", "Có lỗi xảy ra");
      console.error("Error:", error);
    });
}

function updateCartBadge(count) {
  const badge = document.querySelector(".header-cart .cart-badge");
  if (badge) {
    badge.textContent = count;
    if (count > 0) {
      badge.classList.remove("d-none");
    } else {
      badge.classList.add("d-none");
    }
  }
}

// Show/Hide password toggle function
function togglePassword(inputId, button) {
  const input = document.getElementById(inputId);
  if (!input) return;
  const icon = button.querySelector("i");
  if (input.type === "password") {
    input.type = "text";
    if (icon) {
      icon.className = "bi bi-eye-slash";
    }
  } else {
    input.type = "password";
    if (icon) {
      icon.className = "bi bi-eye";
    }
  }
}

// Mini Cart Drawer operations
function openMiniCart() {
  const drawer = document.getElementById("miniCartDrawer");
  const overlay = document.getElementById("miniCartOverlay");
  const container = document.getElementById("miniCartContainer");

  if (drawer && overlay && container) {
    container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-teal" role="status"></div></div>';
    drawer.classList.add("open");
    overlay.classList.add("open");

    fetch("/ShoppingCart/GetMiniCart")
      .then(res => res.text())
      .then(html => {
        container.innerHTML = html;
        // Also update badge in case it changed
        const itemRows = container.querySelectorAll(".mini-cart-item");
        let count = 0;
        itemRows.forEach(row => {
          const qtyText = row.querySelector(".quantity-selector-sm span");
          if (qtyText) count += parseInt(qtyText.textContent) || 0;
        });
        updateCartBadge(count);
      })
      .catch(err => {
        container.innerHTML = '<div class="text-center py-5 text-danger"><i class="bi bi-exclamation-triangle"></i> Lỗi tải giỏ hàng</div>';
        console.error(err);
      });
  }
}

function closeMiniCart() {
  const drawer = document.getElementById("miniCartDrawer");
  const overlay = document.getElementById("miniCartOverlay");
  if (drawer && overlay) {
    drawer.classList.remove("open");
    overlay.classList.remove("open");
  }
}

function updateMiniCartQty(productId, qty) {
  if (qty < 1) {
    removeMiniCartItem(productId);
    return;
  }
  fetch(`/ShoppingCart/UpdateQuantity?productId=${productId}&quantity=${qty}`)
    .then(() => {
      openMiniCart();
    })
    .catch(err => console.error(err));
}

function removeMiniCartItem(productId) {
  fetch(`/ShoppingCart/RemoveFromCart?productId=${productId}`)
    .then(() => {
      openMiniCart();
    })
    .catch(err => console.error(err));
}

// Live Autocomplete Search implementation
document.addEventListener("DOMContentLoaded", function () {
  const searchInput = document.getElementById("headerSearchInput");
  const suggestionsBox = document.getElementById("searchSuggestions");

  if (searchInput && suggestionsBox) {
    let debounceTimer;

    searchInput.addEventListener("input", function () {
      clearTimeout(debounceTimer);
      const query = this.value.trim();

      if (query.length < 2) {
        suggestionsBox.innerHTML = "";
        suggestionsBox.classList.add("d-none");
        return;
      }

      debounceTimer = setTimeout(() => {
        fetch(`/Product/Autocomplete?term=${encodeURIComponent(query)}`)
          .then(res => res.json())
          .then(data => {
            if (data && data.length > 0) {
              suggestionsBox.innerHTML = "";
              data.forEach(item => {
                const imgHtml = item.imageUrl 
                  ? `<img src="${item.imageUrl}" class="suggestion-img" alt="${item.name}" />`
                  : `<div style="width:40px; height:40px; background:#eee;" class="d-flex align-items-center justify-content-center text-muted"><i class="bi bi-image"></i></div>`;
                
                const itemLink = document.createElement("a");
                itemLink.href = `/Product/Details/${item.id}`;
                itemLink.className = "suggestion-item";
                itemLink.innerHTML = `
                  ${imgHtml}
                  <div class="suggestion-info">
                    <span class="suggestion-name">${item.name}</span>
                    <span class="suggestion-price">${item.price.toLocaleString("vi-VN")}đ</span>
                  </div>
                `;
                suggestionsBox.appendChild(itemLink);
              });
              suggestionsBox.classList.remove("d-none");
            } else {
              suggestionsBox.innerHTML = '<div class="p-3 text-muted text-center small">Không tìm thấy sản phẩm</div>';
              suggestionsBox.classList.remove("d-none");
            }
          })
          .catch(err => console.error("Autocomplete error:", err));
      }, 300);
    });

    // Close suggestion box on clicking outside
    document.addEventListener("click", function (e) {
      if (!searchInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
        suggestionsBox.classList.add("d-none");
      }
    });
  }
});
