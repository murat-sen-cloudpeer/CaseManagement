import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CaseInstance } from '../models/case-instance.model';
import { SearchCaseInstancesResult } from '../models/search-case-instances.model';
import { CaseFileItem } from '../models/case-file-item.model';

@Injectable()
export class CaseInstancesService {
    constructor(private http: HttpClient) { }

    search(caseDefinitionId: string, startIndex: number, count: number, order: string, direction: string): Observable<SearchCaseInstancesResult>{
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        let targetUrl = process.env.API_URL + "/case-instances/search?start_index=" + startIndex + "&count=" + count + "&case_definition_id=" + caseDefinitionId;
        if (order) {
            targetUrl = targetUrl + "&order_by=" + order;
        }

        if (direction) {
            targetUrl = targetUrl + "&order=" + direction;
        }

        return this.http.get(targetUrl, { headers: headers }).pipe(map((res: any) => {
            var result = SearchCaseInstancesResult.fromJson(res);
            return result;
        }));
    }

    get(id: string): Observable<CaseInstance> {
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        let targetUrl = process.env.API_URL + "/case-instances/" + id;
        return this.http.get(targetUrl, { headers: headers }).pipe(map((res: any) => {
            return CaseInstance.fromJson(res);
        }));
    }

    getCaseFileItems(id: string): Observable<CaseFileItem[]> {
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        let targetUrl = process.env.API_URL + "/case-instances/" + id + "/casefileitems";
        return this.http.get(targetUrl, { headers: headers }).pipe(map((res: any) => {
            let result: CaseFileItem[] = [];
            res["casefileitems"].forEach(function (cf: any) {
                result.push(CaseFileItem.fromJson(cf));
            });

            return result;
        }));
    }

    create(caseDefId: string): Observable<CaseInstance> {
        const request = JSON.stringify({ case_definition_id: caseDefId });
        let targetUrl = process.env.API_URL + "/case-instances";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        headers = headers.set('Content-Type', 'application/json');
        return this.http.post<Observable<CaseInstance>>(targetUrl, request, { headers: headers }).pipe(map((res: any) => {
            return CaseInstance.fromJson(res);
        }));
    }

    launch(caseInstanceId : string) {
        let targetUrl = process.env.API_URL + "/case-instances/" + caseInstanceId + "/launch";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        return this.http.get(targetUrl, { headers: headers });
    }

    reactivateCaseInstance(caseInstanceId: string) {
        let targetUrl = process.env.API_URL + "/case-instances/" + caseInstanceId + "/reactivate";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        return this.http.get(targetUrl, { headers: headers });
    }

    suspendCaseInstance(caseInstanceId: string) {
        let targetUrl = process.env.API_URL + "/case-instances/" + caseInstanceId + "/suspend";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        return this.http.get(targetUrl, { headers: headers });
    }

    resumeCaseInstance(caseInstanceId: string) {
        let targetUrl = process.env.API_URL + "/case-instances/" + caseInstanceId + "/resume";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        return this.http.get(targetUrl, { headers: headers });
    }

    closeCaseInstance(caseInstanceId: string) {
        let targetUrl = process.env.API_URL + "/case-instances/" + caseInstanceId + "/close";
        let headers = new HttpHeaders();
        headers = headers.set('Accept', 'application/json');
        return this.http.get(targetUrl, { headers: headers });
    }
}