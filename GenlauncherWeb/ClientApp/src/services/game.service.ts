import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";

@Injectable({
  providedIn: 'root',
})
export class GameService {
  constructor(private http: HttpClient) {

  }

  startGame() {
    return this.http.get('/api/game/start');
  }
}
