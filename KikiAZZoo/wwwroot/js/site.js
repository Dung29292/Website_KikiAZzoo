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

// Smart Multi-Category Live Autocomplete Search implementation
document.addEventListener("DOMContentLoaded", function () {
  function setupSmartSearch(inputId, boxId) {
    const searchInput = document.getElementById(inputId);
    const suggestionsBox = document.getElementById(boxId);

    if (!searchInput || !suggestionsBox) return;

    let debounceTimer;

    function escapeHtml(str) {
      if (!str) return "";
      return str.replace(/[&<>"']/g, function (m) {
        return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m];
      });
    }

    function highlightTerm(text, term) {
      if (!text || !term) return escapeHtml(text);
      const escapedTerm = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
      const regex = new RegExp(`(${escapedTerm})`, "gi");
      return escapeHtml(text).replace(regex, '<mark class="search-highlight">$1</mark>');
    }

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
            suggestionsBox.innerHTML = "";

            // Handle structured response { products, pets, totalProducts, totalPets, term }
            const products = data.products || (Array.isArray(data) ? data : []);
            const pets = data.pets || [];
            const hasProducts = products && products.length > 0;
            const hasPets = pets && pets.length > 0;

            if (!hasProducts && !hasPets) {
              suggestionsBox.innerHTML = `
                <div class="p-3 text-center">
                  <div class="text-muted small mb-2"><i class="ph ph-magnifying-glass fs-4 d-block mb-1"></i>Không tìm thấy kết quả phù hợp cho <strong>"${escapeHtml(query)}"</strong></div>
                  <div class="small text-secondary" style="font-size:0.78rem;">Gợi ý: Thử gõ <em>poodle, mèo, thức ăn, pate, cát...</em></div>
                </div>`;
              suggestionsBox.classList.remove("d-none");
              return;
            }

            let html = '<div class="smart-search-results-wrapper">';

            // 1. Live Pets Section (Top priority if pets matched!)
            if (hasPets) {
              html += `
                <div class="smart-search-header d-flex align-items-center justify-content-between px-3 pt-2 pb-1">
                  <span class="small fw-bold text-teal"><i class="ph-fill ph-paw-print me-1"></i> THÚ CƯNG & CÁ CẢNH (${data.totalPets || pets.length})</span>
                  <a href="/thu-cung?search=${encodeURIComponent(query)}" class="small fw-semibold text-orange text-decoration-none">Xem tất cả →</a>
                </div>
                <div class="smart-search-pet-list">`;

              pets.forEach(pet => {
                const img = pet.imageUrl || "/images/pet-dog.svg";
                const priceFormatted = Number(pet.price || 0).toLocaleString("vi-VN") + "đ";
                html += `
                  <a href="/thu-cung/Details/${pet.id}" class="suggestion-item suggestion-pet-item">
                    <img src="${img}" class="suggestion-img suggestion-pet-img" alt="${escapeHtml(pet.name)}" />
                    <div class="suggestion-info flex-grow-1">
                      <div class="d-flex align-items-center gap-1">
                        <span class="suggestion-name">${highlightTerm(pet.name, query)}</span>
                        <span class="badge bg-teal text-white rounded-pill ms-auto" style="font-size:0.62rem;">${escapeHtml(pet.breedName || "Thú cưng")}</span>
                      </div>
                      <div class="d-flex align-items-center justify-content-between mt-1">
                        <span class="suggestion-price">${priceFormatted}</span>
                        <span class="small text-muted" style="font-size:0.72rem;">${pet.gender || ""} • ${pet.ageMonths || 2} tháng</span>
                      </div>
                    </div>
                  </a>`;
              });

              html += '</div>';
            }

            // Divider if both exist
            if (hasPets && hasProducts) {
              html += '<hr class="my-1 border-secondary border-opacity-10" />';
            }

            // 2. Merchandise Products Section
            if (hasProducts) {
              html += `
                <div class="smart-search-header d-flex align-items-center justify-content-between px-3 pt-2 pb-1">
                  <span class="small fw-bold text-secondary"><i class="ph ph-storefront me-1"></i> SẢN PHẨM & PHỤ KIỆN (${data.totalProducts || products.length})</span>
                  <a href="/Product?search=${encodeURIComponent(query)}" class="small fw-semibold text-teal text-decoration-none">Xem tất cả →</a>
                </div>
                <div class="smart-search-product-list">`;

              products.forEach(p => {
                const img = p.imageUrl || "/images/product-food.svg";
                const priceFormatted = Number(p.price || 0).toLocaleString("vi-VN") + "đ";
                const categoryBadge = p.categoryName ? `<span class="badge bg-light text-muted border ms-auto" style="font-size:0.65rem;">${escapeHtml(p.categoryName)}</span>` : "";

                html += `
                  <a href="/Product/Details/${p.id}" class="suggestion-item">
                    <img src="${img}" class="suggestion-img" alt="${escapeHtml(p.name)}" />
                    <div class="suggestion-info flex-grow-1">
                      <div class="d-flex align-items-center gap-1">
                        <span class="suggestion-name">${highlightTerm(p.name, query)}</span>
                        ${categoryBadge}
                      </div>
                      <div class="d-flex align-items-center justify-content-between mt-1">
                        <span class="suggestion-price">${priceFormatted}</span>
                        ${p.originalPrice && p.originalPrice > p.price ? `<span class="small text-decoration-line-through text-muted" style="font-size:0.7rem;">${Number(p.originalPrice).toLocaleString("vi-VN")}đ</span>` : ""}
                      </div>
                    </div>
                  </a>`;
              });

              html += '</div>';
            }

            // 3. Footer Search Actions
            html += `
              <div class="smart-search-footer p-2 bg-light d-flex align-items-center justify-content-between border-top">
                <a href="/Product?search=${encodeURIComponent(query)}" class="btn btn-sm btn-outline-teal rounded-pill py-1 px-3 fw-semibold" style="font-size:0.76rem;">
                  <i class="ph ph-storefront me-1"></i>Tìm Sản phẩm
                </a>
                <a href="/thu-cung?search=${encodeURIComponent(query)}" class="btn btn-sm btn-teal rounded-pill py-1 px-3 fw-semibold" style="font-size:0.76rem;">
                  <i class="ph-fill ph-paw-print me-1"></i>Tìm Thú cưng
                </a>
              </div>`;

            html += '</div>';

            suggestionsBox.innerHTML = html;
            suggestionsBox.classList.remove("d-none");
          })
          .catch(err => {
            console.error("Autocomplete error:", err);
          });
      }, 250);
    });

    // Close on click outside
    document.addEventListener("click", function (e) {
      if (!searchInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
        suggestionsBox.classList.add("d-none");
      }
    });

    // Close on Escape key
    searchInput.addEventListener("keydown", function (e) {
      if (e.key === "Escape") {
        suggestionsBox.classList.add("d-none");
      }
    });
  }

  // Setup both desktop and mobile header search
  setupSmartSearch("headerSearchInput", "searchSuggestions");
  setupSmartSearch("mobileSearchInput", "mobileSearchSuggestions");
});
