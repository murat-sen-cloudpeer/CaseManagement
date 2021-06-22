﻿import { Component, ViewEncapsulation, OnInit, OnDestroy } from '@angular/core';
import { GuidGenerator } from './guidgenerator';
import * as fromHumanTaskDefActions from '@app/stores/humantaskdefs/actions/humantaskdef.actions';
import { ActivatedRoute } from '@angular/router';
import { Store, select, ScannedActionsSubject } from '@ngrx/store';
import * as fromAppState from '@app/stores/appstate';
import { HumanTaskDef } from '@app/stores/humantaskdefs/models/humantaskdef.model';
import { TranslateService } from '@ngx-translate/core';
import { MatSnackBar } from '@angular/material';
import { filter } from 'rxjs/operators';
import { FormGroup } from '@angular/forms';

@Component({
    selector: 'view-humantaskdef-rendering-component',
    templateUrl: './rendering.component.html',
    styleUrls: ['./rendering.component.scss'],
    encapsulation: ViewEncapsulation.None
})
export class ViewHumanTaskDefRenderingComponent implements OnInit, OnDestroy {
    option: any = null;
    formGroup: FormGroup = new FormGroup({});
    uiOption: any = {
        editMode: true
    };
    humanTaskListener: any;

    constructor(
        private route: ActivatedRoute,
        private store: Store<fromAppState.AppState>,
        private snackBar: MatSnackBar,
        private translateService: TranslateService,
        private actions$: ScannedActionsSubject) { }

    ngOnInit() {
        this.actions$.pipe(
            filter((action: any) => action.type === fromHumanTaskDefActions.ActionTypes.COMPLETE_UPDATE_RENDERING))
            .subscribe(() => {
                this.snackBar.open(this.translateService.instant('HUMANTASK.MESSAGES.RENDERING_UPDATED'), this.translateService.instant('undo'), {
                    duration: 2000
                });
            });
        this.actions$.pipe(
            filter((action: any) => action.type === fromHumanTaskDefActions.ActionTypes.ERROR_UPDATE_RENDERING))
            .subscribe(() => {
                this.snackBar.open(this.translateService.instant('HUMANTASK.MESSAGES.ERROR_UPDATE_RENDERING'), this.translateService.instant('undo'), {
                    duration: 2000
                });
            });
        this.humanTaskListener = this.store.pipe(select(fromAppState.selectHumanTaskResult)).subscribe((e: HumanTaskDef) => {
            if (!e) {
                return;
            }

            this.option = e.rendering;
        });
    }

    ngOnDestroy() {
        if(!this.humanTaskListener) {
            this.humanTaskListener.unsubscribe();
        }
    }

    dragColumn(evt: any) {
        const json: any = {
            id: GuidGenerator.newGUID(),
            type: 'row',
            children: [
                { id: GuidGenerator.newGUID(), type: 'column', children: [] },
                { id: GuidGenerator.newGUID(), type: 'column', children: [] }
            ]
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragTxt(evt: any) {
        const json : any = {
            id: GuidGenerator.newGUID(),
            name: 'txt_' + GuidGenerator.newGUID(),
            type: 'txt',
            validationRules: [],
            translations: []
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragPwd(evt: any) {
        if (this.getUniqueChild(this.option, 'pwd') !== null) {
            return;
        }

        const json: any = {
            id: GuidGenerator.newGUID(),
            name: 'pwd',
            type: 'pwd',
            validationRules: [],
            translations: []
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragConfirmPwd(evt: any) {
        if (this.getUniqueChild(this.option, 'confirmpwd') !== null) {
            return;
        }

        const json: any = {
            id: GuidGenerator.newGUID(),
            name: 'confirmpwd',
            type: 'confirmpwd',
            validationRules: ['CONFIRMPWD'],
            translations: []
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragSelect(evt: any) {
        const json = {
            id: GuidGenerator.newGUID(),
            type: 'select',
            label: 'Label',
            name: 'select_' + GuidGenerator.newGUID()
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragHeader(evt: any) {
        const json = {
            id: GuidGenerator.newGUID(),
            type: 'header',
            txt: 'Header',
            class: 'mat-display-1'
        };
        evt.dataTransfer.setData('json', JSON.stringify(json));
    }

    dragOver(evt: any) {
        evt.preventDefault();
    }

    dropColumn(evt: any) {
        const str = evt.dataTransfer.getData('json');
        if (!str) {
            return;
        }

        const json = JSON.parse(evt.dataTransfer.getData('json'));
        this.option.children.push(json);
    }

    save() {
        const id = this.route.parent.snapshot.params['id'];
        const request = new fromHumanTaskDefActions.UpdateRenderingOperation(id, this.option);
        this.store.dispatch(request);
    }

    private getUniqueChild(opt: any, name: string) : any {
        if (!opt.children) {
            return null;
        }

        for (let i = 0; i < opt.children.length; i++) {
            const child = opt.children[i];
            if (child.name === name) {
                return child;
            }

            const record = this.getUniqueChild(child, name);
            if (record !== null) {
                return record;
            }
        }

        return null;
    }
}