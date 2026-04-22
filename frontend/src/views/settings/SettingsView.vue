<script setup>
import { Bell, Building, CreditCard, Shield, User } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
</script>

<template>
  <div class="view-page">
    <div class="space-y-6">

      <div class="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <section class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2">
              <User class="h-4 w-4 text-blue-600" />
              <h3 class="text-sm font-bold uppercase tracking-widest text-slate-900">Profile</h3>
            </div>
            <Button class="h-8 bg-blue-600 px-3 text-[11px] font-bold uppercase tracking-wider text-white hover:bg-blue-700">
              Save
            </Button>
          </div>

          <div class="space-y-5">
            <div class="flex items-center gap-4">
              <div class="flex h-16 w-16 items-center justify-center rounded-2xl border-2 border-dashed border-slate-200 bg-slate-100 text-slate-400">
                <User class="h-7 w-7" />
              </div>
              <div>
                <Button variant="outline" class="h-8 border-slate-200 px-3 text-[11px] font-bold uppercase tracking-widest">
                  Change Photo
                </Button>
                <p class="mt-1.5 text-[11px] text-slate-400">Recommended: 400x400px JPG or PNG</p>
              </div>
            </div>

            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Full Name</Label>
                <Input value="Warrick" class="h-10" />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Email Address</Label>
                <Input type="email" value="user@example.com" class="h-10" />
              </div>
            </div>
          </div>
        </section>

        <section class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2">
              <Building class="h-4 w-4 text-blue-600" />
              <h3 class="text-sm font-bold uppercase tracking-widest text-slate-900">Studio Identity</h3>
            </div>
            <Button class="h-8 bg-blue-600 px-3 text-[11px] font-bold uppercase tracking-wider text-white hover:bg-blue-700">
              Save
            </Button>
          </div>

          <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div class="space-y-1.5">
              <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Studio Name</Label>
              <Input value="ArchAutomate Studio" class="h-10" />
            </div>
            <div class="space-y-1.5">
              <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Registration Number</Label>
              <Input value="SACAP-0000" class="h-10" />
            </div>
            <div class="space-y-1.5 md:col-span-2">
              <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Address</Label>
              <Input value="123 Main Road, Cape Town" class="h-10" />
            </div>
          </div>
        </section>

        <section class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2">
              <Shield class="h-4 w-4 text-blue-600" />
              <h3 class="text-sm font-bold uppercase tracking-widest text-slate-900">Security</h3>
            </div>
            <Button variant="outline" class="h-8 border-slate-200 px-3 text-[11px] font-bold uppercase tracking-wider">
              Update
            </Button>
          </div>

          <div class="space-y-4">
            <div class="space-y-1.5">
              <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Current Password</Label>
              <Input type="password" placeholder="••••••••" class="h-10" />
            </div>
            <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">New Password</Label>
                <Input type="password" placeholder="Minimum 8 characters" class="h-10" />
              </div>
              <div class="space-y-1.5">
                <Label class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Confirm Password</Label>
                <Input type="password" placeholder="Repeat new password" class="h-10" />
              </div>
            </div>
          </div>
        </section>

        <section class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2">
              <Bell class="h-4 w-4 text-blue-600" />
              <h3 class="text-sm font-bold uppercase tracking-widest text-slate-900">Notifications</h3>
            </div>
            <Button variant="outline" class="h-8 border-slate-200 px-3 text-[11px] font-bold uppercase tracking-wider">
              Save
            </Button>
          </div>

          <div class="space-y-3 text-sm text-slate-700">
            <label class="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2.5">
              <span>Council submission updates</span>
              <input type="checkbox" checked class="h-4 w-4 rounded border-slate-300" />
            </label>
            <label class="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2.5">
              <span>Rejection alerts</span>
              <input type="checkbox" checked class="h-4 w-4 rounded border-slate-300" />
            </label>
            <label class="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2.5">
              <span>Weekly summary email</span>
              <input type="checkbox" class="h-4 w-4 rounded border-slate-300" />
            </label>
          </div>
        </section>

        <section class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm xl:col-span-2">
          <div class="mb-5 flex items-center justify-between border-b border-slate-100 pb-4">
            <div class="flex items-center gap-2">
              <CreditCard class="h-4 w-4 text-blue-600" />
              <h3 class="text-sm font-bold uppercase tracking-widest text-slate-900">Subscription</h3>
            </div>
            <Button variant="outline" class="h-8 border-slate-200 px-3 text-[11px] font-bold uppercase tracking-wider">
              Manage Billing
            </Button>
          </div>

          <div class="grid grid-cols-1 gap-3 md:grid-cols-3">
            <div class="rounded-lg border border-slate-200 bg-slate-50 p-4">
              <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Current Plan</p>
              <p class="mt-1 text-sm font-semibold text-slate-900">Professional</p>
            </div>
            <div class="rounded-lg border border-slate-200 bg-slate-50 p-4">
              <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Renewal Date</p>
              <p class="mt-1 text-sm font-semibold text-slate-900">30 May 2026</p>
            </div>
            <div class="rounded-lg border border-slate-200 bg-slate-50 p-4">
              <p class="text-[10px] font-bold uppercase tracking-widest text-slate-400">Users</p>
              <p class="mt-1 text-sm font-semibold text-slate-900">5 of 10 seats used</p>
            </div>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>
