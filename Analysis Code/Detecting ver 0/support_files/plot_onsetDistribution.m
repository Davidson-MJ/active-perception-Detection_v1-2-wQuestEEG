function  plot_onsetDistribution(dataIN, cfg)
% helper function to plot either the distribution of all target onsets, or
% all response onsets (clicks), relative to gait "%"

% called from the script 
% plot_ReactionTime_

GFX_headY = cfg.HeadData;
usecols = {[0 .7 0], [.7 0 0], [.7 0 .7]}; % R Gr Prp

%Note that this plots both target and response onset relative to gait.
% For the response ver, we also overlay all False Alarms recorded
useFA =strcmp(cfg.type, 'Response');
if cfg.usebin
    useFA=0; % dont plot FA on binned x axis
end

figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9 .9]);
nsubs = length(cfg.subjIDs);
%% set up shortcuts for accessing data:
binfields = {'', '_binned'}; 
usefield = [binfields{cfg.usebin+1} '_counts'];
usegaitnames= {'gc','doubgc'};

 for ippant = 1:nsubs  
         clf;
        pc=1; % plot counter        
%         pspots = [1,4, 7, 2,5, 6]; %suplot order
        psubj= cfg.subjIDs{ippant}(1:2); % print ppid.
         % both this and the next use the same figure function:
pspots=1:6;
        for nGaits_toPlot=1:2
            
            
            
            
            if nGaits_toPlot==1
                pidx= cfg.pidx1;
                ftnames= {'LR', 'RL', 'combined'};
            else
                pidx= cfg.pidx2;
                ftnames= {'LRL', 'RLR', 'combined'};
            end
            
            legp=[]; % for legend
            for iLR=1:3
                    
                ppantData= dataIN(ippant,iLR).([usegaitnames{nGaits_toPlot} usefield]);
                plotHead = GFX_headY(ippant).([usegaitnames{nGaits_toPlot}]);
                 if useFA
                     faData = dataIN(ippant,iLR).([usegaitnames{nGaits_toPlot} '_FAs']);
                 end
                
              %x axis:
              if cfg.usebin
                  
                  %x axis:          %approx centre point of the binns.
                  mdiff = round(mean(diff(pidx)./2));
                  xvec = pidx(1:end-1) + mdiff;
              else  % note that if we aren't using the binned versions, use full xaxis.
                  %         (overwrite the above)
                  xvec = 1:pidx(end);
              end
        
                %%
                subplot(2,3,pspots(pc))
                hold on;
                yyaxis left
                
               % finely sampled bar, each gait "%" point.
                bh=bar(xvec, ppantData);
                bh.FaceColor = usecols{iLR};
                legp(iLR)= bh;
                
                ylabel([cfg.type ' onset [counts]']);                
%% add FA if response type:

                if useFA
                     bh=bar(xvec, faData);
                     bh.FaceColor = 'm';                   
                end
%%
                
                
                yyaxis right
                ph=plot(plotHead, ['k-o'], 'linew', 3); hold on
                set(gca,'ytick', []);
                if cfg.usebin==0
                title([psubj ' (N ' num2str(nansum(ppantData)) ') ' ftnames{iLR}], 'interpreter', 'none');
                else
                    title([psubj ' ' ftnames{iLR}], 'interpreter', 'none');
                end
                    
                midp=xvec(ceil(length(xvec)/2));
                set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})
                
                xlabel([ '% of gait-cycle ']);%
                ylim([0 max(plotHead)]);
                pc=pc+1; %plotcounter
                
% pspots=pspots+1;
            end % i LR
            
        end % nGaits.
     %%  
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' onset distribution'])
        shg
        
        print([psubj ' ' cfg.type ' onset distribution binned(' num2str(cfg.usebin) ')'],'-dpng');
    end % ppant

end