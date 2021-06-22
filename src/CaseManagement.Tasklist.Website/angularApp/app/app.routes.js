export var routes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    { path: 'home', loadChildren: './home/home.module#HomeModule' },
    { path: 'tasks', loadChildren: './tasks/tasks.module#TasksModule' },
    { path: 'notifications', loadChildren: './notifications/notifications.module#NotificationsModule' },
    { path: 'cases', loadChildren: './cases/cases.module#CasesModule' },
    { path: 'status', loadChildren: './status/status.module#StatusModule' },
    { path: '**', redirectTo: '/status/404' }
];
//# sourceMappingURL=app.routes.js.map