import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { AuthService } from './app/core/auth.service';

bootstrapApplication(AppComponent, appConfig).then((ref) => {
  const auth = ref.injector.get(AuthService);
  auth.login().subscribe();
})
  .catch((err) => console.error(err));
