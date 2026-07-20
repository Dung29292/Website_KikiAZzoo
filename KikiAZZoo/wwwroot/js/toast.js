// Toast Notification System
class ToastNotification {
  static container = null;

  static init() {
    if (!this.container) {
      this.container = document.createElement("div");
      this.container.className = "toast-container-custom";
      document.body.appendChild(this.container);
    }
  }

  static show(title, message, type = "success", duration = 3000) {
    this.init();

    const toast = document.createElement("div");
    toast.className = `toast-notification ${type}`;

    const iconMap = {
      success: '<i class="bi bi-check-circle-fill"></i>',
      error: '<i class="bi bi-exclamation-circle-fill"></i>',
      warning: '<i class="bi bi-exclamation-triangle-fill"></i>',
      info: '<i class="bi bi-info-circle-fill"></i>',
    };

    toast.innerHTML = `
      <div class="toast-content">
        <div class="toast-icon">${iconMap[type] || iconMap["info"]}</div>
        <div class="toast-text">
          <div class="toast-title">${title}</div>
          <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" aria-label="Close">&times;</button>
      </div>
    `;

    this.container.appendChild(toast);

    const closeBtn = toast.querySelector(".toast-close");
    closeBtn.addEventListener("click", () => this.remove(toast));

    if (duration > 0) {
      setTimeout(() => this.remove(toast), duration);
    }

    return toast;
  }

  static remove(toast) {
    toast.classList.add("removing");
    setTimeout(() => {
      if (toast.parentNode) {
        toast.parentNode.removeChild(toast);
      }
    }, 400);
  }

  static success(title, message) {
    return this.show(title, message, "success");
  }

  static error(title, message) {
    return this.show(title, message, "error");
  }

  static warning(title, message) {
    return this.show(title, message, "warning");
  }

  static info(title, message) {
    return this.show(title, message, "info");
  }
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", function () {
  ToastNotification.init();
});
