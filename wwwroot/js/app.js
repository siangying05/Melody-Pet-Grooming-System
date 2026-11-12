// wwwroot/js/app.js

function initLoader() {
    const loader = document.querySelector('.loader');
    if (loader) {
        setTimeout(() => {
            loader.style.opacity = '0';
            setTimeout(() => {
                loader.style.display = 'none';
            }, 300);
        }, 500);
    }
}

function initNavigation() {
    const navToggle = document.querySelector('.nav-toggle');
    const navLinks = document.querySelector('.nav-links');
    const navAuth = document.querySelector('.nav-auth');

    if (!navToggle || !navLinks) return;

    function setupResponsiveNav() {
        if (window.innerWidth <= 768) {
            if (!document.querySelector('.nav-links .login-btn')) {
                if (navAuth && navAuth.querySelector('.login-btn')) {
                    const loginBtnClone = navAuth.querySelector('.login-btn').cloneNode(true);
                    loginBtnClone.classList.add('mobile-login-btn');
                    navLinks.appendChild(loginBtnClone);
                }
            }
        } else {
            const mobileLoginBtn = document.querySelector('.nav-links .mobile-login-btn');
            if (mobileLoginBtn) {
                mobileLoginBtn.remove();
            }
        }
    }

    setupResponsiveNav();
    window.addEventListener('resize', setupResponsiveNav);

    navToggle.addEventListener('click', function () {
        navLinks.classList.toggle('active');
        navToggle.classList.toggle('active');
        document.body.style.overflow = navLinks.classList.contains('active') ? 'hidden' : '';
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.nav-container')) {
            navLinks.classList.remove('active');
            navToggle.classList.remove('active');
            document.body.style.overflow = '';
        }
    });

    navLinks.addEventListener('click', function (event) {
        if (event.target.tagName === 'A') {
            navLinks.classList.remove('active');
            navToggle.classList.remove('active');
            document.body.style.overflow = '';
        }
    });
}

function highlightCurrentPage() {
    const currentPath = window.location.pathname;
    document.querySelectorAll('.nav-link').forEach(link => {
        const linkPath = link.getAttribute('href');
        if (linkPath === currentPath) {
            link.classList.add('active');
        }
    });
}

function initPageTransitions() {
    window.addEventListener('beforeunload', function () {
        const loader = document.querySelector('.loader');
        if (loader) {
            loader.style.display = 'block';
            loader.style.opacity = '1';
        }
    });
}

function initFormValidation() {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            const requiredFields = form.querySelectorAll('[required]');
            let isValid = true;

            requiredFields.forEach(field => {
                if (!field.value.trim()) {
                    isValid = false;
                    field.classList.add('error');
                } else {
                    field.classList.remove('error');
                }
            });

            if (!isValid) e.preventDefault();
        });
    });
}

function initImageCarousel() {
    const carousel = document.querySelector('.scroll-carousel');
    if (carousel) {
        console.log('Carousel found, initialize here');
    }
}

function initToastAutoHide() {
    const toasts = document.querySelectorAll('.toast');
    toasts.forEach(toast => {
        setTimeout(() => toast.classList.add('hide'), 10000);
        setTimeout(() => toast.remove(), 10500);
    });
}
function togglePassword() {
    const input = document.getElementById("passwordInput");
    if (!input) return;
    input.type = input.type === "password" ? "text" : "password";
}

function initAvatarUpload() {
    const avatarUpload = document.getElementById('avatarUpload');
    const avatarImage = document.getElementById('avatarImage');

    if (avatarUpload && avatarImage) {
        avatarUpload.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (!file) return;

            if (!file.type.match('image.*')) {
                alert('Please select an image file (JPG, PNG, GIF)');
                return;
            }

            const formData = new FormData();
            formData.append('file', file);

            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) formData.append('__RequestVerificationToken', token.value);

            avatarImage.style.opacity = '0.5';

            fetch('/Account/UploadAvatar', { method: 'POST', body: formData })
                .then(response => {
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.json();
                })
                .then(data => {
                    if (data.success) avatarImage.src = data.url;
                    else alert('Error: ' + data.message);
                })
                .catch(() => alert('Error uploading avatar. Please try again.'))
                .finally(() => avatarImage.style.opacity = '1');
        });

        avatarUpload.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                const reader = new FileReader();
                reader.onload = e => avatarImage.src = e.target.result;
                reader.readAsDataURL(this.files[0]);
            }
        });
    }
}

function initCaptchaRefresh() {
    const captcha = document.getElementById("captchaImage");
    if (captcha) {
        captcha.addEventListener("click", function () {
            this.src = "/Captcha/GetCaptcha?" + new Date().getTime();
        });
    }
}

function initApp() {
    initLoader();
    initNavigation();
    highlightCurrentPage();
    initPageTransitions();
    initFormValidation();
    initImageCarousel();
    initToastAutoHide();
    initAvatarUpload();
    initCaptchaRefresh();
    console.log('Melody Pet Grooming App initialized');
}

// DOMContentLoaded 安全初始化
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initApp);
} else {
    initApp();
}

// jQuery 部分，统一判空
if (typeof $ !== 'undefined') {
    $(document).on('click', '[data-get]', e => {
        e.preventDefault();
        const url = e.target.dataset.get;
        location = url || location;
    });

    $(document).on('click', '[data-post]', e => {
        e.preventDefault();
        const url = e.target.dataset.post;
        const f = $('<form>').appendTo(document.body)[0];
        f.method = 'post';
        f.action = url || location;
        f.submit();
    });

    $('[data-trim]').on('change', e => e.target.value = e.target.value.trim());

    $('[data-upper]').on('input', e => {
        const a = e.target.selectionStart;
        const b = e.target.selectionEnd;
        e.target.value = e.target.value.toUpperCase();
        e.target.setSelectionRange(a, b);
    });

    $('[type=reset]').on('click', e => {
        e.preventDefault();
        location = location;
    });

    $('[data-check]').on('click', e => {
        e.preventDefault();
        const name = e.target.dataset.check;
        if (name) $(`[name=${name}]`).prop('checked', true);
    });

    $('[data-uncheck]').on('click', e => {
        e.preventDefault();
        const name = e.target.dataset.uncheck;
        if (name) $(`[name=${name}]`).prop('checked', false);
    });

    $(document).on('click', '[data-checkable]', e => {
        if ($(e.target).is(':input,a')) return;
        $(e.currentTarget).find(':checkbox').prop('checked', (i, v) => !v);
    });

    $('.upload input').on('change', e => {
        const f = e.target.files[0];
        const img = $(e.target).siblings('img')[0];
        if (!img) return;

        img.dataset.src ??= img.src;

        if (f && f.type.startsWith('image/')) {
            img.onload = () => URL.revokeObjectURL(img.src);
            img.src = URL.createObjectURL(f);
        } else {
            img.src = img.dataset.src;
            e.target.value = '';
        }

        $(e.target).valid?.();
    });
}

// module exports
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { initApp, initNavigation, highlightCurrentPage };
}
window.togglePassword = togglePassword;
