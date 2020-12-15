var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { Component } from '@angular/core';
import { FormBuilder, FormControl } from '@angular/forms';
import * as fromAppState from '@app/stores/appstate';
import * as fromCmmnFileActions from '@app/stores/cmmnfiles/actions/cmmn-files.actions';
import { CmmnFile } from '@app/stores/cmmnfiles/models/cmmn-file.model';
import { select, Store } from '@ngrx/store';
var ViewCmmnFileInformationComponent = (function () {
    function ViewCmmnFileInformationComponent(store, formBuilder) {
        this.store = store;
        this.formBuilder = formBuilder;
        this.cmmnFile = new CmmnFile();
        this.saveForm = this.formBuilder.group({
            id: new FormControl({ value: '', disabled: true }),
            name: new FormControl({ value: '' }),
            createDateTime: new FormControl({ value: '', disabled: true }),
            updateDateTime: new FormControl({ value: '', disabled: true }),
            description: new FormControl({ value: '' })
        });
    }
    ViewCmmnFileInformationComponent.prototype.ngOnInit = function () {
        var _this = this;
        this.store.pipe(select(fromAppState.selectCmmnFileResult)).subscribe(function (e) {
            if (!e) {
                return;
            }
            _this.cmmnFile = e;
            _this.saveForm.controls['id'].setValue(e.id);
            _this.saveForm.controls['name'].setValue(e.name);
            _this.saveForm.controls['createDateTime'].setValue(e.createDateTime);
            _this.saveForm.controls['updateDateTime'].setValue(e.updateDateTime);
            _this.saveForm.controls['description'].setValue(e.description);
        });
    };
    ViewCmmnFileInformationComponent.prototype.onSave = function (form) {
        var id = this.saveForm.get('id').value;
        var act = new fromCmmnFileActions.UpdateCmmnFile(id, form.name, form.description);
        this.store.dispatch(act);
    };
    ViewCmmnFileInformationComponent = __decorate([
        Component({
            selector: 'view-cmmn-information-file',
            templateUrl: './information.component.html',
            styleUrls: ['./information.component.scss']
        }),
        __metadata("design:paramtypes", [Store,
            FormBuilder])
    ], ViewCmmnFileInformationComponent);
    return ViewCmmnFileInformationComponent;
}());
export { ViewCmmnFileInformationComponent };
//# sourceMappingURL=information.component.js.map