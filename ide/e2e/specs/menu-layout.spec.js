module.exports = [{ id: 'menu-fixed-check-column', name: 'В меню только команды, названия не сдвигаются при включении галочек', async fn({ S, C }) {
  await S.launch();
  C.eq(await S.page.locator('#menubar .shell-brand').count(), 0, 'В строке меню нет надписи A / AXIOM');
  await S.page.locator('[data-menu="edit"] .menu-label').click();
  for (const key of ['formatOnBlur', 'formatOnSave', 'checkOnChange']) {
    const layout = await S.page.evaluate(key => {
      const check = document.querySelector('.menu-check[data-check="' + key + '"]');
      const text = check.parentElement.querySelector('.menu-text');
      check.classList.remove('on'); const off = text.getBoundingClientRect().x;
      check.classList.add('on'); const on = text.getBoundingClientRect().x;
      return { off, on, width: check.getBoundingClientRect().width };
    }, key);
    C.eq(layout.off, layout.on, 'Название неподвижно: ' + key);
    C.ok(layout.width >= 16, 'Место для галочки зарезервировано: ' + key);
  }
} }];
