<script setup lang="ts">
import { useMessagesStore } from '@/stores/messages';
import { Button, InputText, Panel, Textarea, useToast } from 'primevue';
import { ref } from 'vue';
import * as z from "zod"; 

interface Recipient {
    contactTypeId: number,
    contactData: string
}

const toast = useToast()
const messagesStore = useMessagesStore() 

const email = ref('')
const subject = ref('')
const message = ref('')

const parseError = ref<null | string>(null)

const recipients = ref<Recipient[]>([])

const MsgSchema = z.object({
    recipients: z
        .array(
            z.object({
                contactTypeId: z.number(),
                contactData: z.email("Некорректный формат email")
            })
        )
        .min(1, "Добавьте хотя бы одного получателя"),
    subject: z
        .string()
        .trim()
        .nonempty("Введите тему сообщения"),
    message: z
        .string()
        .nonempty("Введите текст сообщения")
})

const sendMsg = async () => {
    parseError.value = null
    const result = MsgSchema.safeParse({
        recipients: recipients.value,
        subject: subject.value,
        message: message.value
    })

    if (!result.success) {
        parseError.value = result.error.issues[0]?.message as string ?? 'Ошибка валидации'
    } else {
        try {
            await messagesStore.sendMsgAsync(0, {
                subject: subject.value,
                storageTimeAfterSendingInHours: 24,
                messageBody: message.value,
                recipients: recipients.value
            })

            email.value = ''
            subject.value = ''
            message.value = ''
            recipients.value = []
        } catch (error) {
            toast.add({ severity: 'error', summary: 'Неизвестная ошибка', detail: `${error}` });
        }
    }
}

const addRecipient = (recipient: Recipient) => {
    if (recipients.value.filter(rec => rec == recipient).length) {
        toast.add({ severity: 'error', summary: 'Ошибка', detail: 'Такой получатель уже существует', life: 1500 })
    } else if (!email.value) {
        toast.add({ severity: 'error', summary: 'Ошибка', detail: 'Введите адрес получателя', life: 1500 })
    } else {
        recipients.value.push(recipient)
        email.value = ''
    }
}

const removeRecipient = (recipientToRemove: Recipient) => {
    recipients.value = recipients.value.filter(recipient => recipient !== recipientToRemove)
}

</script>

<template>
    <Panel header="Отправить сообщение">
        <form class="sendForm" @submit.prevent="sendMsg">
            <div class="recipientsForm">
                <InputText :disabled="messagesStore.isMsgSending.value" inputmode="email" placeholder="Email получателя" v-model="email" />
                <Button label="+" @click="addRecipient({contactTypeId: 1, contactData: email})" type="button" />
            </div>

            <div class="recipients">
                <div v-for="recipient in recipients" class="recipient">
                        {{ recipient.contactData }}
                        <Button 
                            icon="pi pi-times" 
                            variant="text" 
                            rounded 
                            severity="danger"
                            @click="removeRecipient(recipient)"
                            type="button"
                        />
                </div>
            </div>

            <InputText :disabled="messagesStore.isMsgSending.value" placeholder="Тема" v-model="subject" />
            <Textarea :disabled="messagesStore.isMsgSending.value" autoResize placeholder="Текст сообщения" v-model="message" />
    
            <p v-if="parseError" class="error">{{ parseError }}</p>

            <Button 
                label="Отправить" 
                class="sendButton" 
                type="submit" 
                :disabled="messagesStore.isMsgSending.value"
            />
        </form>
    </Panel>
</template>

<style scoped>
.p-panel {
    width: fit-content;
    max-width: 500px;
}

.sendForm {
    display: flex;
    flex-direction: column;
    width: fit-content;
    gap: 8px;
}

.sendButton {
    margin-top: 8px;
}

.recipientsForm {
    display: flex;
    gap: 8px;
}

.recipients {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.recipient {
    width: fit-content;
}

@media (max-width: 768px) {
    .p-panel {
        max-width: 250px;
    }
}
</style>
