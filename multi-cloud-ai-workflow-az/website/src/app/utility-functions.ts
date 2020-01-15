import { BehaviorSubject, Observable, of } from "rxjs";
import { switchMap, tap } from "rxjs/operators";

export function emitOnceAndCache<T>(subject: BehaviorSubject<T>, load: Observable<T>): Observable<T> {
    return subject.asObservable().pipe(switchMap(x => !!x ? of(x) : load.pipe(tap(t => subject.next(t)))));
}