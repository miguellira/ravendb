﻿/// <reference path="../models/dto.ts" />
import dialogViewModelBase = require("viewmodels/dialogViewModelBase");
import simulateSqlReplicationCommand = require("commands/simulateSqlReplicationCommand");
import database = require("models/database");
import document = require("models/document");
import getDocumentsMetadataByIDPrefixCommand = require("commands/getDocumentsMetadataByIDPrefixCommand");
import dialog = require("plugins/dialog");
import collection = require("models/collection");
import sqlReplication = require("models/sqlReplication");

class sqlReplicationSimulationDialog extends dialogViewModelBase {

    simulationResults = ko.observable<string[]>([]);
    rolledBackTransactionPassed = ko.observable<boolean>(false);
    documentAutocompletes = ko.observableArray<string>();
    documentId = ko.observable<string>();
    lastSearchedDocumentID = ko.observable<string>("");
    isAutoCompleteVisible: KnockoutComputed<boolean>;
    rolledbackTransactionPerformed = ko.observable<boolean>(false);
    lastAlert = ko.observable<string>("");
    
    constructor(private db: database, private simulatedSqlReplication: sqlReplication) {
        super();
        this.documentId.throttle(250).subscribe(search => this.fetchDocumentIdAutocompletes(search));
        this.isAutoCompleteVisible = ko.computed(() => {
            return this.lastSearchedDocumentID() !== this.documentId() && 
                (this.documentAutocompletes().length > 1 || this.documentAutocompletes().length == 1 && this.documentId() !== this.documentAutocompletes()[0]);
        });
    }

    getResults(performRolledbackTransaction: boolean) {
        this.rolledbackTransactionPerformed(performRolledbackTransaction);
        this.lastSearchedDocumentID(this.documentId());
        new simulateSqlReplicationCommand(this.db, this.simulatedSqlReplication, this.documentId(), performRolledbackTransaction)
            .execute()
            .done((result: sqlReplicationSimulationResultDto) => {
                if (!!result.Results) {
                    this.simulationResults(result.Results.map(x=> x.Commands.map(y=> y.CommandText)).reduce((x,y) =>x.concat(y)));
                    this.rolledBackTransactionPassed(!result.LastAlert);
                }

                if (!!result.LastAlert) {
                    this.lastAlert(result.LastAlert.Exception);
                } else {
                    this.lastAlert("");
                }

            })
            .fail(() => {
                this.simulationResults([]);
                this.rolledBackTransactionPassed(false);
        });
    }

    // overrid dialogViewModelBase shortcuts behavior
   attached() {
       $("#docIdInput").focus();
       var that = this;
       jwerty.key("esc", e => {
           e.preventDefault();
           dialog.close(that);
       }, this, this.dialogSelectorName == "" ? dialogViewModelBase.dialogSelector : this.dialogSelectorName);
   }

    fetchDocumentIdAutocompletes(query: string) {
        if (query.length >= 2) {
            new getDocumentsMetadataByIDPrefixCommand(query, 10, this.db)
                .execute()
                .done((results: string[]) => {
                    if (this.documentId() === query) {
                        if (results.length == 1 && this.documentId() == results[0]) {
                            this.documentAutocompletes.removeAll();
                            return;
                        }
                        this.documentAutocompletes(results);
                    }
                });
        } else if (query.length == 0) {
            this.documentAutocompletes.removeAll();
        }
    }

    documentIdSubmitted(submittedDocumentId) {
        this.documentId(submittedDocumentId);
        $('#docIdInput').focus();
        this.getResults(false);
    }

    getDocCssClass(doc: documentMetadataDto) {
        return collection.getCollectionCssClass(doc['@metadata']['Raven-Entity-Name']);
    }

    keyPressedOnDocumentAutocomplete(doc: documentMetadataDto, event) {
        if (event.keyCode == 13 && !!doc) {
            var docId = !!doc['@metadata'] ? doc['@metadata']['@id'] : null;
            if (!!docId) {
                this.documentId(docId);
            }
        }
    }

    cancel() {
        dialog.close(this);
    }


}

export = sqlReplicationSimulationDialog;