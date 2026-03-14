// ElAI Solutions — site.js

(function () {
  'use strict';

  // ============================================================
  // 1. NAVBAR SCROLL SHADOW
  // ============================================================
  var navbar = document.querySelector('.navbar-marketing');
  if (navbar) {
    var handleScroll = function () {
      if (window.scrollY > 10) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    };
    window.addEventListener('scroll', handleScroll, { passive: true });
    handleScroll(); // run on load
  }

  // ============================================================
  // 2. COOKIE CONSENT
  // ============================================================
  var cookieBanner = document.getElementById('cookie-banner');
  var cookieAccept = document.getElementById('cookie-accept');
  var cookieDecline = document.getElementById('cookie-decline');

  if (cookieBanner) {
    var consent = localStorage.getItem('cookieConsent');
    if (!consent) {
      // Small delay so the banner doesn't flash before page render
      setTimeout(function () {
        cookieBanner.style.display = 'block';
      }, 800);
    }

    if (cookieAccept) {
      cookieAccept.addEventListener('click', function () {
        localStorage.setItem('cookieConsent', 'accepted');
        cookieBanner.style.display = 'none';
      });
    }

    if (cookieDecline) {
      cookieDecline.addEventListener('click', function () {
        localStorage.setItem('cookieConsent', 'declined');
        cookieBanner.style.display = 'none';
      });
    }
  }

  // ============================================================
  // 3. SMOOTH SCROLL FOR ANCHOR LINKS
  // ============================================================
  document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
    anchor.addEventListener('click', function (e) {
      var targetId = this.getAttribute('href');
      if (targetId === '#') return;
      var target = document.querySelector(targetId);
      if (target) {
        e.preventDefault();
        var offset = navbar ? navbar.offsetHeight + 16 : 16;
        var top = target.getBoundingClientRect().top + window.pageYOffset - offset;
        window.scrollTo({ top: top, behavior: 'smooth' });
      }
    });
  });

  // ============================================================
  // 4. PRICING TOGGLE (monthly / annual)
  // ============================================================
  var billingToggle = document.querySelector('.billing-toggle');
  var priceAmounts = document.querySelectorAll('.price-amount');
  var labelMonthly = document.getElementById('label-monthly');
  var labelAnnual = document.getElementById('label-annual');
  var billingNote = document.getElementById('billing-note');

  if (billingToggle && priceAmounts.length > 0) {
    var isAnnual = false;

    var updatePrices = function () {
      priceAmounts.forEach(function (el) {
        var monthly = el.getAttribute('data-monthly');
        var annual = el.getAttribute('data-annual');
        if (isAnnual && annual) {
          el.textContent = annual;
        } else if (monthly) {
          el.textContent = monthly;
        }
      });

      if (labelMonthly) {
        labelMonthly.classList.toggle('active', !isAnnual);
      }
      if (labelAnnual) {
        labelAnnual.classList.toggle('active', isAnnual);
      }
      if (billingNote) {
        billingNote.textContent = isAnnual
          ? 'Jaarlijks gefactureerd — 10% korting toegepast.'
          : 'Maandelijks gefactureerd. Schakel over naar jaarlijks voor 10% korting.';
      }
    };

    billingToggle.addEventListener('change', function () {
      isAnnual = this.checked;
      updatePrices();
    });

    updatePrices(); // run on load to set initial state
  }

  // ============================================================
  // 5. CONTACT FORM — JS-only submit (no backend yet)
  // ============================================================
  var contactForm = document.getElementById('contact-form');
  var contactSuccess = document.getElementById('contact-success');
  var contactSubmitBtn = document.getElementById('contact-submit');

  if (contactSubmitBtn && contactForm) {
    contactSubmitBtn.addEventListener('click', function () {
      // Basic HTML5 validation check
      if (!contactForm.checkValidity()) {
        contactForm.reportValidity();
        return;
      }

      // TODO: Connect to email service (e.g. SendGrid, Resend, or ASP.NET POST handler)
      // For now, show success message and reset form
      contactForm.reset();
      contactForm.style.display = 'none';
      if (contactSuccess) {
        contactSuccess.style.display = 'block';
      }
    });
  }

})();
