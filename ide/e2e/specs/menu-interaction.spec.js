module.exports=[{id:'menu-persistent-diagonal',name:'Меню остаётся открытым при уходе мыши и переходе по диагонали',async fn({S,C}){
 await S.launch();const tools=S.page.locator('[data-menu="tools"]');await tools.locator('.menu-label').click();
 await S.page.mouse.move(850,600);await S.sleep(900);C.ok(await tools.evaluate(el=>el.classList.contains('open')),'Уход мыши не закрывает Сервис');
 const trigger=S.page.locator('#formatting-submenu .submenu-trigger');await trigger.hover();const submenu=S.page.locator('#formatting-submenu .submenu-dropdown');
 await S.page.mouse.move(850,600);await S.sleep(900);C.ok(await submenu.isVisible(),'Подменю сохраняется без наведения');
 const target=await S.page.locator('[data-action="format-modern"]').boundingBox();await S.page.mouse.move(target.x+target.width/2,target.y+target.height/2,{steps:12});await S.page.mouse.click(target.x+target.width/2,target.y+target.height/2);
 await S.page.waitForFunction(()=>window.__ide.settings.formattingStyle==='Modern');C.ok(true,'Движение по диагонали позволяет выбрать режим');C.ok(!(await tools.evaluate(el=>el.classList.contains('open'))),'Команда закрывает меню');
 await tools.locator('.menu-label').click();await trigger.click();C.ok(await submenu.isVisible(),'Щелчок по заголовку подменю не закрывает меню');await S.page.keyboard.press('Escape');C.ok(!(await submenu.isVisible()),'Escape закрывает меню');
 await tools.locator('.menu-label').click();await S.page.mouse.click(900,500);C.ok(!(await tools.evaluate(el=>el.classList.contains('open'))),'Щелчок в редакторе закрывает меню');
 await tools.locator('.menu-label').click();await S.page.locator('[data-menu="file"] .menu-label').hover();C.ok(await S.page.locator('[data-menu="file"]').evaluate(el=>el.classList.contains('open')),'Наведение переключает открытое меню на Файл');
 await S.page.keyboard.press('Escape');await tools.locator('.menu-label').hover();C.ok(!(await tools.evaluate(el=>el.classList.contains('open'))),'После закрытия случайное наведение не открывает меню');
}}];
