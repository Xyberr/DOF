<script setup lang="ts">
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import { useAsyncState } from '@vueuse/core';
import { MessagesService, type MessageResponse } from '@/heyapi';
import { ref } from 'vue';
import Panel from 'primevue/panel';
import { Button, Tag, useToast } from 'primevue';
import { useMessagesStore } from '@/stores/messages';

const messagesStore = useMessagesStore()

const msgs = ref<MessageResponse[]>([])

const toast = useToast()

const { isLoading: isMsgsLoading, execute: getMsgsAsync } = useAsyncState(
    async () => {
        return MessagesService.getMessages()
    },
    null,
    {
        immediate: true,
        resetOnExecute: false,
        throwError: true,
        onSuccess(data) {
            msgs.value = data?.data ?? []
        }
    },
)

const formatDate = (value: string) => {
    if (!value) return '';
    const date = new Date(value);
    return date.toLocaleString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    });
};

const updateMsgs = async () => {
    await getMsgsAsync(0)
}

const resendMsg = async (rowData: MessageResponse, recipient: any) => {
    try {
        await messagesStore.sendMsgAsync(0, {
            subject: rowData.subject,
            storageTimeAfterSendingInHours: 24,
            messageBody: rowData.messageBody,
            recipients: [
                {
                    contactData: recipient.contactData,
                    contactTypeId: recipient.contactTypeId
                }
            ]
        })

        toast.add({
            severity: 'success',
            summary: 'Успешно',
            detail: `Сообщение для ${recipient.contactData} переотправлено`
        })

        await updateMsgs();
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Ошибка', detail: error instanceof Error ? error.message : `${error}` });
    }
}

const getStatusText = (statusId: number) => {
    const statuses: Record<number, string> = {
        1: 'В очереди',
        2: 'Обрабатывается',
        3: 'Успех',
        4: 'Ошибка'
    };
    return statuses[statusId] || `Статус №${statusId}`;
};

const getStatusSeverity = (statusId: number) => {
    const severities: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
        1: 'info',
        2: 'secondary',
        3: 'success',
        4: 'danger'
    };
    return severities[statusId] || 'secondary';
};

definePage({
    meta: {
        needAuth: true,
    }
});
</script>

<template>
    <main class="jobsPage">
        <Panel header="Отправления">
            <template #icons>
                <Button icon="pi pi-sync" severity="secondary" rounded text @click="updateMsgs" />
            </template>

            <DataTable :value="msgs" tableStyle="min-width: 50rem" :paginator="true" :rows="10"
                :rowsPerPageOptions="[10, 25, 50, 100]">
                <Column field="id" header="ID"></Column>
                <Column header="Получатели">
                    <template #body="slotProps">
                        <div class="recipients">
                            <div v-for="recipient in slotProps.data.recipients" :key="recipient.id">
                                {{ recipient.contactData }}
                                <!-- <Button 
                                   icon="pi pi-refresh" 
                                   variant="text" 
                                   rounded 
                                   severity="danger"
                                   @click="resendMsg(slotProps.data, recipient)"
                                   type="button"
                                   v-if="recipient.deliveryStatusId !== 3"
                                />
                                <span class="pi pi-check" v-if="recipient.deliveryStatusId == 3"></span> -->
                            </div>
                        </div>
                    </template>
                </Column>
                <Column field="subject" header="Тема"></Column>
                <Column field="messageBody" header="Сообщение" class="max-width-col">
                   <template #body="slotProps">
                        <div class="message-text">
                            {{ slotProps.data.messageBody }}
                        </div>
                    </template>
                </Column>
                <Column header="Отправлено">
                    <template #body="slotProps">
                        {{ formatDate(slotProps.data.createdAt) }}
                    </template>
                </Column>

                <Column header="Статус отправки">
                    <template #body="slotProps">
                        <div class="statuses">
                            <div v-for="recipient in slotProps.data.recipients" :key="recipient.id">
                                <Tag :severity="getStatusSeverity(recipient.deliveryStatusId)"
                                    :value="getStatusText(recipient.deliveryStatusId)" />
                            </div>
                        </div>
                    </template>
                </Column>
            </DataTable>
        </Panel>
    </main>
</template>

<style scoped>
.jobsPage {}

:deep(.max-width-col) {
    max-width: 100px;
}

.message-text {
    white-space: normal; 
    word-break: break-word;
    max-height: 100px; 
    overflow-y: auto;  
    overflow-x: hidden;
}

.recipients {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.statuses {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.pages {
    display: flex;
    gap: 8px;
    justify-content: center;
    margin-top: 16px;
}

span {
    color: #5de05d;
    margin-left: 8px;
}

@media (min-width: 1024px) {
    :deep(.max-width-col) {
        max-width: 300px;
    }
}
</style>