import { BehaviorSubject, Observable, of } from "rxjs";
import { switchMap, tap, share, first } from "rxjs/operators";

export function emitOnceAndCache<T>(subject: BehaviorSubject<T>, load: Observable<T>): Observable<T> {
    load = load.pipe(first(), share());
    return subject.asObservable().pipe(switchMap(x => !!x ? of(x) : load.pipe(tap(t => subject.next(t)))));
}