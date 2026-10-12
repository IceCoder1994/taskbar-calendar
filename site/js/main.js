/**
 * 任务栏日历 (Taskbar Calendar) 官网交互脚本
 * 功能：深浅色主题切换持久化、预览 Tab 键盘可访问切换、滚动揭示动效、导航区块高亮
 * 说明：主题初始化已在 <head> 内联脚本中同步完成（防闪烁），此处只负责切换与状态同步。
 */

/* --------------------------------------------------------------------------
   1. 预览图片预加载：切换 Tab 时先解码再替换，避免白屏闪烁
   -------------------------------------------------------------------------- */
const imageCache = new Map();

function preloadImage(src) {
  if (imageCache.has(src)) {
    return imageCache.get(src);
  }
  const task = new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(src);
    img.onerror = () => reject(new Error('图片加载失败: ' + src));
    img.src = src;
  });
  // 失败结果不进缓存，允许用户重试
  task.catch(() => imageCache.delete(src));
  imageCache.set(src, task);
  return task;
}

/* --------------------------------------------------------------------------
   2. 主题切换：与 head 内联脚本保持同一套优先级（localStorage > 系统偏好）
   -------------------------------------------------------------------------- */
function initThemeToggle() {
  const toggleBtn = document.getElementById('theme-toggle');
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)');

  // 同步按钮的无障碍文案，让读屏用户知道当前状态与点击后果
  function syncToggleLabel() {
    if (!toggleBtn) return;
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const label = isDark ? '切换到浅色外观' : '切换到深色外观';
    toggleBtn.setAttribute('title', label);
    toggleBtn.setAttribute('aria-label', label);
  }

  syncToggleLabel();

  if (toggleBtn) {
    toggleBtn.addEventListener('click', () => {
      const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
      document.documentElement.setAttribute('data-theme', isDark ? 'light' : 'dark');
      try {
        localStorage.setItem('theme', isDark ? 'light' : 'dark');
      } catch (e) {
        // 隐私模式下 localStorage 可能不可用，忽略即可
      }
      syncToggleLabel();
    });
  }

  // 用户未手动选择过主题时，跟随系统变化
  prefersDark.addEventListener('change', (event) => {
    let hasManualChoice = false;
    try {
      hasManualChoice = Boolean(localStorage.getItem('theme'));
    } catch (e) {
      hasManualChoice = false;
    }
    if (!hasManualChoice) {
      document.documentElement.setAttribute('data-theme', event.matches ? 'dark' : 'light');
      syncToggleLabel();
    }
  });
}

/* --------------------------------------------------------------------------
   3. Hero 预览 Tab：支持鼠标点击与键盘左右方向键切换
   -------------------------------------------------------------------------- */
function initPreviewTabs() {
  const tabs = Array.from(document.querySelectorAll('.preview-tab'));
  const previewImg = document.getElementById('preview-img');
  const previewPanel = document.getElementById('preview-panel');

  if (!tabs.length || !previewImg) return;

  let activeIndex = tabs.findIndex((tab) => tab.classList.contains('active'));
  if (activeIndex < 0) activeIndex = 0;

  function activateTab(index, moveFocus) {
    const tab = tabs[index];
    if (!tab) return;

    activeIndex = index;

    tabs.forEach((item, i) => {
      const isCurrent = i === index;
      item.classList.toggle('active', isCurrent);
      item.setAttribute('aria-selected', isCurrent ? 'true' : 'false');
      // 仅当前 Tab 可被 Tab 键聚焦，方向键在组内移动（WAI-ARIA tabs 模式）
      item.setAttribute('tabindex', isCurrent ? '0' : '-1');
    });

    if (moveFocus) tab.focus();

    if (previewPanel) {
      previewPanel.setAttribute('aria-labelledby', tab.id);
    }

    const fileName = tab.getAttribute('data-img');
    if (!fileName) return;

    const targetSrc = 'assets/' + fileName;

    preloadImage(targetSrc)
      .then(() => {
        previewImg.style.opacity = '0';
        previewImg.style.transform = 'scale(0.98)';
        window.setTimeout(() => {
          previewImg.src = targetSrc;
          previewImg.style.opacity = '1';
          previewImg.style.transform = 'scale(1)';
        }, 140);
      })
      .catch(() => {
        // 预加载失败时直接替换，让浏览器原生行为兜底
        previewImg.src = targetSrc;
      });
  }

  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => activateTab(index, false));

    tab.addEventListener('keydown', (event) => {
      let nextIndex = null;

      if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
        nextIndex = (index + 1) % tabs.length;
      } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
        nextIndex = (index - 1 + tabs.length) % tabs.length;
      } else if (event.key === 'Home') {
        nextIndex = 0;
      } else if (event.key === 'End') {
        nextIndex = tabs.length - 1;
      }

      if (nextIndex !== null) {
        event.preventDefault();
        activateTab(nextIndex, true);
      }
    });
  });
}

/* --------------------------------------------------------------------------
   4. 滚动揭示：元素进入视口时错峰淡入上移
   -------------------------------------------------------------------------- */
function initScrollReveal() {
  const targets = Array.from(document.querySelectorAll('.reveal'));
  if (!targets.length) return;

  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // 标记动效已接管，head 中的兜底逻辑据此判断主脚本是否正常执行
  document.documentElement.setAttribute('data-reveal-ready', 'true');

  if (reduceMotion || !('IntersectionObserver' in window)) {
    targets.forEach((el) => el.classList.add('is-visible'));
    return;
  }

  const observer = new IntersectionObserver((entries) => {
    // 同批进入视口的元素按索引错峰，形成自上而下的节奏感
    entries
      .filter((entry) => entry.isIntersecting)
      .forEach((entry, index) => {
        const delay = Math.min(index * 70, 280);
        window.setTimeout(() => entry.target.classList.add('is-visible'), delay);
        observer.unobserve(entry.target);
      });
  }, {
    rootMargin: '0px 0px -12% 0px',
    threshold: 0.08
  });

  targets.forEach((el) => observer.observe(el));

  // 兜底：headless 截图 / iframe 渲染节流 / 省电模式冻结渲染帧等极端环境下，
  // IntersectionObserver 可能始终不回调，元素会永久停留在隐藏态导致内容隐身。
  // 3 秒后无条件显示全部，确保任何环境下内容都不会丢失。
  window.setTimeout(() => {
    targets.forEach((el) => el.classList.add('is-visible'));
    observer.disconnect();
  }, 3000);
}

/* --------------------------------------------------------------------------
   5. 导航区块高亮：滚动到对应区块时点亮导航项
   -------------------------------------------------------------------------- */
function initNavHighlight() {
  const navLinks = Array.from(document.querySelectorAll('.nav-links a[href^="#"]'));
  if (!navLinks.length) return;

  const linkBySectionId = new Map();
  const sections = [];

  navLinks.forEach((link) => {
    const id = link.getAttribute('href').slice(1);
    const section = id ? document.getElementById(id) : null;
    if (section) {
      linkBySectionId.set(id, link);
      sections.push(section);
    }
  });

  if (!sections.length || !('IntersectionObserver' in window)) return;

  function setActive(id) {
    navLinks.forEach((link) => link.classList.remove('is-active'));
    const target = linkBySectionId.get(id);
    if (target) target.classList.add('is-active');
  }

  // 取视口上部区域作为判定带：区块顶部进入该带即视为"当前区块"
  const observer = new IntersectionObserver((entries) => {
    const visible = entries.filter((entry) => entry.isIntersecting);
    if (!visible.length) return;
    // 多个区块同时命中时，取最靠上的那个
    visible.sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
    setActive(visible[0].target.id);
  }, {
    rootMargin: '-20% 0px -70% 0px',
    threshold: 0
  });

  sections.forEach((section) => observer.observe(section));
}

/* --------------------------------------------------------------------------
   6. 启动
   -------------------------------------------------------------------------- */
document.addEventListener('DOMContentLoaded', () => {
  initThemeToggle();
  initPreviewTabs();
  initScrollReveal();
  initNavHighlight();
});
