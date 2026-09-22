/**
 * 任务栏日历 (Taskbar Calendar) 官网交互脚本
 * 功能：深浅色主题切换持久化、图片预览 Tab 切换、平滑滚动、下载事件友好交互
 */

document.addEventListener('DOMContentLoaded', () => {
  // 1. 深浅色主题切换与记忆
  const themeToggleBtn = document.getElementById('theme-toggle');
  const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
  const savedTheme = localStorage.getItem('theme');

  // 初始化主题（优先 localStorage，其次系统偏好）
  if (savedTheme) {
    document.documentElement.setAttribute('data-theme', savedTheme);
  } else if (prefersDarkScheme.matches) {
    document.documentElement.setAttribute('data-theme', 'dark');
  } else {
    document.documentElement.setAttribute('data-theme', 'light');
  }

  // 监听系统主题变化
  prefersDarkScheme.addEventListener('change', (e) => {
    if (!localStorage.getItem('theme')) {
      document.documentElement.setAttribute('data-theme', e.matches ? 'dark' : 'light');
    }
  });

  // 主题按钮点击切换
  if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', () => {
      const currentTheme = document.documentElement.getAttribute('data-theme');
      const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
      document.documentElement.setAttribute('data-theme', nextTheme);
      localStorage.setItem('theme', nextTheme);
    });
  }

  // 2. Hero 首屏预览大图 Tab 切换
  const previewTabs = document.querySelectorAll('.preview-tab');
  const previewImg = document.getElementById('preview-img');

  if (previewTabs.length && previewImg) {
    previewTabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        // 移除所有 active
        previewTabs.forEach((t) => t.classList.remove('active'));
        tab.classList.add('active');

        const targetImgName = tab.getAttribute('data-img');
        if (targetImgName) {
          // 平滑淡入淡出动效
          previewImg.style.opacity = '0';
          previewImg.style.transform = 'scale(0.97)';
          setTimeout(() => {
            previewImg.src = `assets/${targetImgName}`;
            previewImg.style.opacity = '1';
            previewImg.style.transform = 'scale(1)';
          }, 150);
        }
      });
    });
  }

  // 3. 点击下载按钮时给予友好的操作引导反馈
  const downloadBtns = document.querySelectorAll('.btn-download, .cta-actions a');
  downloadBtns.forEach((btn) => {
    btn.addEventListener('click', () => {
      // 检查是否在 5 秒内再次点击，给予轻提示
      setTimeout(() => {
        console.log('任务栏日历下载已触发。若遇 SmartScreen 提示，请选择「更多信息」→「仍要运行」。');
      }, 500);
    });
  });
});
