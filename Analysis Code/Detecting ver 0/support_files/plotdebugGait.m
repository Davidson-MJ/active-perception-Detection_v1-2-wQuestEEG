% % plotdebugGait% quick plot to check the information extracted per gait:
figure(1); clf;
% 
% 

% plot raw and resampled, check correct info extracted.
headY = cfg.HeadPos(itrial).Y;
trialTime = cfg.HeadPos(itrial).times;
tmpTarg = cfg.TargState(itrial).state;
tmpClick = cfg.clickState(itrial).state;
gaitsamps = gd(igait).gaitsamps;
subplot(211);
plot(trialTime, headY, 'k'); hold on;
plot(trialTime(gaitsamps), headY(gaitsamps), 'r-', 'linew', 2)
yyaxis right
plot(trialTime, tmpTarg, 'b-');
plot(trialTime, tmpClick, 'b:');

title(['trial ' num2str(itrial) ', ' subjID])
%
subplot(223); title(['gait ' num2str(igait)])
plot(gd(igait).Head_Ynorm, 'k'); hold on;
subplot(224)
plot(gd(igait).Head_Y_resampled, 'r'); shg
if ~isempty(gd(igait).tOnset_inGait)
    tOs = gd(igait).tOnset_inGait;
    for itg= 1:length(tOs)
        subplot(223); hold on;
plot([tOs(itg), tOs(itg)], ylim,'color', 'k');
subplot(224); hold on;
plot([gd(igait).tOnset_inGaitResampled(itg), gd(igait).tOnset_inGaitResampled(itg)], ylim, 'color', 'r');
    end
end
if ~isempty(gd(igait).response_rawsamp)
    tRs=gd(igait).response_rawsamp;
    for itR=1:length(tRs)
        subplot(223); hold on
plot([tRs(itR),tRs(itR)], ylim,'k:');
subplot(224); hold on
plot([gd(igait).response_resamp(itR), gd(igait).response_resamp(itR)], ylim, 'r:');
    end
end
title(['gait: ' num2str(igait) ' gcounter: ' num2str(gcounter)]);

%%
% figure(2);
% plot(gd(igait).Head_Y_resampled, 'k-'); hold on;


%% also store the info, for comparison with our table output:
% gaitRpcnts=[];
% if isfield(gd(igait),'response_resamp')&& ~isempty(gd(igait).response_resamp) && igait~=1
%     tRs=gd(igait).response_resamp;
%     
%     for itR=1:length(tRs)
%        
% %         plot([gd(igait).response_resamp(itR), gd(igait).response_resamp(itR)], ylim, 'r:');
%         
%          allRpcnt=[allRpcnt,gd(igait).response_resamp(itR) ]; %% plotted afterwards.
%         
%         gaitRpcnts = [gaitRpcnts,gd(igait).response_resamp(itR) ];
%     end
% end
%all trial response resamps.

%     alldGC=[alldGC;gd(igait).Head_Y_resampled ];
